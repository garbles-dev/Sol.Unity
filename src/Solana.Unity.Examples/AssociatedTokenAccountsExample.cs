using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Solana.Unity.Examples
{
    public class AssociatedTokenAccountsExample : IExample
    {

        private static readonly IRpcClient RpcClient = ClientFactory.GetClient(Cluster.TestNet);

        private const string MnemonicWords =
            "route clerk disease box emerge airport loud waste attitude film army tray " +
            "forward deal onion eight catalog surface unit card window walnut wealth medal";

        public async void Run()
        {
            Wallet.Wallet wallet = new Wallet.Wallet(MnemonicWords);

            /*
             * The following region creates and initializes a mint account, it also creates a token account
             * that is initialized with the same mint account and then mints tokens to this newly created token account.
             */
            #region Create and Initialize a token Mint Account


            ulong minBalanceForExemptionAcc =
                (await RpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize)).Result;
            ulong minBalanceForExemptionMint =
                (await RpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize)).Result;

            Console.WriteLine($"MinBalanceForRentExemption Account >> {minBalanceForExemptionAcc}");
            Console.WriteLine($"MinBalanceForRentExemption Mint Account >> {minBalanceForExemptionMint}");

            Account ownerAccount = wallet.GetAccount(10);
            Account mintAccount = wallet.GetAccount(1004);
            Account initialAccount = wallet.GetAccount(1104);
            Console.WriteLine($"OwnerAccount: {ownerAccount}");
            Console.WriteLine($"MintAccount: {mintAccount}");
            Console.WriteLine($"InitialAccount: {initialAccount}");

            string latestBlockHash = RpcClient.GetLatestBlockHashAsync().Result.Result.Value.Blockhash;

            byte[] createAndInitializeMintToTx = new TransactionBuilder().
                SetRecentBlockHash(latestBlockHash).
                SetFeePayer(ownerAccount).
                AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    mintAccount,
                    minBalanceForExemptionMint,
                    TokenProgram.MintAccountDataSize,
                    TokenProgram.ProgramIdKey)).
                AddInstruction(TokenProgram.InitializeMint(
                    mintAccount.PublicKey,
                    2,
                    ownerAccount.PublicKey,
                    ownerAccount.PublicKey)).
                AddInstruction(SystemProgram.CreateAccount(
                    ownerAccount,
                    initialAccount,
                    minBalanceForExemptionAcc,
                    TokenProgram.TokenAccountDataSize,
                    TokenProgram.ProgramIdKey)).
                AddInstruction(TokenProgram.InitializeAccount(
                    initialAccount.PublicKey,
                    mintAccount.PublicKey,
                    ownerAccount.PublicKey)).
                AddInstruction(TokenProgram.MintTo(
                    mintAccount.PublicKey,
                    initialAccount.PublicKey,
                    1_000_000,
                    ownerAccount)).
                AddInstruction(MemoProgram.NewMemo(initialAccount, "Hello from Sol.Net")).
                Build(new List<Account> { ownerAccount, mintAccount, initialAccount });

            string createAndInitializeMintToTxSignature = await Examples.SubmitTxSendAndLog(createAndInitializeMintToTx);

            Examples.PollConfirmedTx(createAndInitializeMintToTxSignature);

            #endregion

            /*
             * The following region creates an associated token account (ATA) for a random account and a certain token mint
             * (in this case it's the previously created token mintAccount) and transfers tokens from the previously created
             * token account to the newly created ATA.
             */
            #region Create Associated Token Account

            // this public key is from a random account created via www.sollet.io
            // to test this locally I recommend creating a wallet on sollet and deriving this
            PublicKey associatedTokenAccountOwner = new PublicKey("65EoWs57dkMEWbK4TJkPDM76rnbumq7r3fiZJnxggj2G");
            PublicKey associatedTokenAccount =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(associatedTokenAccountOwner, mintAccount);
            Console.WriteLine($"AssociatedTokenAccountOwner: {associatedTokenAccountOwner}");
            Console.WriteLine($"AssociatedTokenAccount: {associatedTokenAccount}");

            byte[] createAssociatedTokenAccountTx = new TransactionBuilder().
                SetRecentBlockHash(latestBlockHash).
                SetFeePayer(ownerAccount).
                AddInstruction(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                    ownerAccount,
                    associatedTokenAccountOwner,
                    mintAccount)).
                AddInstruction(TokenProgram.Transfer(
                    initialAccount,
                    associatedTokenAccount,
                    25000,
                    ownerAccount)).// the ownerAccount was set as the mint authority
                AddInstruction(MemoProgram.NewMemo(ownerAccount, "Hello from Sol.Net")).
                Build(new List<Account> { ownerAccount });

            string createAssociatedTokenAccountTxSignature = await Examples.SubmitTxSendAndLog(createAssociatedTokenAccountTx);

            Examples.PollConfirmedTx(createAssociatedTokenAccountTxSignature);

            #endregion
        }
    }
}