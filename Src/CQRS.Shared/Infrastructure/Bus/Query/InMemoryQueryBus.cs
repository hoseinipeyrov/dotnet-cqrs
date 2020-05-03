using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQRS.Shared.Domain.Bus.Query;

namespace CQRS.Shared.Infrastructure.Bus.Query
{
    public class InMemoryQueryBus : QueryBus
    {
        private readonly IServiceProvider _provider;

        public InMemoryQueryBus(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<TResponse> Send<TResponse>(Domain.Bus.Query.Query query)
        {
            var handler = GetWrappedHandlers<TResponse>(query);
            
            if(handler == null) throw new QueryNotRegisteredError(query);

            return await handler.Handle(query);
        }
        
        private QueryHandlerWrapper<TResponse> GetWrappedHandlers<TResponse>(Domain.Bus.Query.Query query)
        {
            Type[] typeArgs = {query.GetType(), typeof(TResponse)};
            
            var handlerType = typeof(QueryHandler<,>).MakeGenericType(typeArgs);
            Type wrapperType = typeof(QueryHandlerWrapper<,>).MakeGenericType(typeArgs);

            IEnumerable handlers =
                (IEnumerable) _provider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType));

            IEnumerable<QueryHandlerWrapper<TResponse>> wrappedHandlers = handlers.Cast<object>()
                .Select(handler => (QueryHandlerWrapper<TResponse>) Activator.CreateInstance(wrapperType, handler));
            
            return wrappedHandlers.FirstOrDefault();
        }
    }
}