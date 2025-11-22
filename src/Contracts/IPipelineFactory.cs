using Faster.EventBus.Contracts;
using System;

namespace Faster.EventBus.Core
{
    /// <summary>
    /// Responsible for analyzing command types and building high-performance
    /// execution delegates (pipelines) used to process commands.
    ///
    /// The pipeline built by this factory includes:
    ///  • All configured <see cref="IPipelineBehavior{TCommand, TResponse}"/> middleware
    ///  • The final <see cref="ICommandHandler{TCommand, TResponse}"/> that handles the command
    ///
    /// The factory does NOT execute pipelines — it only constructs and returns
    /// a fully compiled delegate that can be invoked repeatedly with zero allocations.
    /// Implementations typically use expression trees and caching so that a pipeline
    /// is built only once per command type and reused thereafter.
    ///
    /// Usage example:
    /// <code>
    /// var pipeline = factory.CreateDelegate&lt;Result&gt;(typeof(CreateOrderCommand), provider);
    /// var result = await pipeline(provider, commandInstance, CancellationToken.None);
    /// </code>
    /// </summary>
    public interface IPipelineFactory
    {
        /// <summary>
        /// Creates or retrieves a compiled command execution delegate for the specified command type.
        /// The returned delegate represents the entire command pipeline including behaviors and the core handler.
        /// </summary>
        /// <typeparam name="TResponse">
        /// The type returned by the command handler (e.g., Result, bool, DTO, etc.).
        /// </typeparam>
        /// <param name="commandType">The runtime type of the command being processed.</param>
        /// <param name="rootProvider">
        /// The application's root <see cref="IServiceProvider"/> used for resolving services when building the pipeline.
        /// </param>
        /// <returns>
        /// A cached, pre-compiled <see cref="CommandPipeline{TResponse}"/> delegate ready for execution.
        /// </returns>
        CommandPipeline<TResponse> CreateDelegate<TResponse>(Type commandType, IServiceProvider rootProvider);
    }
}
