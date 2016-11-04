// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBackoffPolicy.cs" company="Microsoft">
//   Copyright (c) Microsoft.  All rights reserved.
// </copyright>
// <summary>
//   Interface for backoff strategy on repeated loops.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DataGenerator.BackoffPolicies
{
    /// <summary>
    ///     Back off policy interface.
    /// </summary>
    internal interface IBackoffPolicy
    {
        /// <summary>
        ///     Await a period based on a strategy.
        /// </summary>
        void Wait();
    }
}