using System;
using System.Collections.Generic;
using System.Text;

namespace Faster.EventBus.Contracts;

public interface IUseCaseDispatcher
{
    ValueTask<TResponse> Execute<TRequest, TResponse>(TRequest request, CancellationToken ct = default);
}

