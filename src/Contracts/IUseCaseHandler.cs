using System;
using System.Collections.Generic;
using System.Text;

namespace Faster.EventBus.Contracts;

public interface IUseCaseHandler<TRequest, TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken ct = default);
}