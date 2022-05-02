﻿using Sol.Unity.Rpc.Core.Http;
using System;

namespace Sol.Unity.Extensions
{
    /// <summary>
    /// An exception thrown brown the TokenWallet that includes the failing Solnet RPC call
    /// </summary>
    public class TokenWalletException : ApplicationException
    {

        /// <summary>
        /// The failing RequestResult that caused the Exception.
        /// </summary>
        public IRequestResult RequestResult { get; private set; }

        /// <summary>
        /// An Exception generated by an RPC failure within the TokenWallet.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="failedResult">The failing RequestResult that caused the exception.</param>
        internal TokenWalletException(string message, IRequestResult failedResult) : base(message)
        {
            RequestResult = failedResult;
        }

    }
}