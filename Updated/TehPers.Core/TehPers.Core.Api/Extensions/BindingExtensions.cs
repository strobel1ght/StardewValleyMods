﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.ContextPreservation;
using Ninject.Parameters;
using Ninject.Syntax;
using TehPers.Core.Api.Conflux;
using TehPers.Core.Api.DependencyInjection;
using TehPers.Core.Api.DependencyInjection.Lifecycle;

namespace TehPers.Core.Api.Extensions
{
    public static class BindingExtensions
    {
        public static IBindingWhenInNamedWithOrOnSyntax<TService> ToService<TService>(this IBindingToSyntax<TService> syntax, Type implementationType)
        {
            _ = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            _ = syntax ?? throw new ArgumentNullException(nameof(syntax));
            return syntax.ToMethod(context => (TService)context.Kernel.Get(implementationType, context.Parameters.ToArray()));
        }

        public static IBindingWhenInNamedWithOrOnSyntax<TImplementation> ToService<TService, TImplementation>(this IBindingToSyntax<TService> syntax)
            where TImplementation : TService
        {
            _ = syntax ?? throw new ArgumentNullException(nameof(syntax));
            return syntax.ToMethod(context => context.Kernel.Get<TImplementation>(context.Parameters.ToArray()));
        }

        public static IParameter[] GetChildParameters(this IContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));
            return context.Parameters.Where(parameter => parameter.ShouldInherit).ToArray();
        }

        public static object ToFirst<TService>(this IBindingToSyntax<TService> syntax, params Type[] implementationTypes)
        {
            _ = implementationTypes ?? throw new ArgumentNullException(nameof(implementationTypes));
            _ = syntax ?? throw new ArgumentNullException(nameof(syntax));
            return syntax.ToMethod(context =>
            {
                var parameters = context.GetChildParameters();
                foreach (var implementationType in implementationTypes)
                {
                    if (context.Kernel.TryGet(implementationType, parameters) is TService result)
                    {
                        return result;
                    }
                }

                throw new ActivationException("None of the implementations could be activated");
            });
        }

        public static object ToFirst<TService, T1>(this IBindingToSyntax<TService> syntax)
            where T1 : TService
        {
            return syntax.ToFirst(typeof(T1));
        }

        public static object ToFirst<TService, T1, T2>(this IBindingToSyntax<TService> syntax)
            where T1 : TService
            where T2 : TService
        {
            return syntax.ToFirst(typeof(T1), typeof(T2));
        }

        public static object ToFirst<TService, T1, T2, T3>(this IBindingToSyntax<TService> syntax)
            where T1 : TService
            where T2 : TService
            where T3 : TService
        {
            return syntax.ToFirst(typeof(T1), typeof(T2), typeof(T3));
        }

        /// <summary>
        /// Binds an API exposed by another mod to your mod's kernel.
        /// </summary>
        /// <typeparam name="T">The type the mod's API returns, or an interface which matches part of (or all of) its signature.</typeparam>
        /// <param name="modKernel">The mod's kernel.</param>
        /// <param name="modId">The foreign mod's API.</param>
        /// <returns>The syntax that can be used to configure the binding.</returns>
        public static IBindingWhenInNamedWithOrOnSyntax<T> BindForeignModApi<T>(this IModKernel modKernel, string modId)
            where T : class
        {
            _ = modId ?? throw new ArgumentNullException(nameof(modId));
            _ = modKernel ?? throw new ArgumentNullException(nameof(modKernel));

            return modKernel.Bind<T>().ToMethod(_ => modKernel.ParentMod.Helper.ModRegistry.GetApi<T>(modId));
        }

        /// <summary>
        /// Binds and registers a service as a handler for all the events it can handle.
        /// </summary>
        /// <typeparam name="TService">The type of service being registered and bound as an event handler.</typeparam>
        /// <param name="kernel">The mod's kernel.</param>
        /// <returns>The syntax that can be used to configure the event handler.</returns>
        public static IBindingInSyntax<TService> BindEventHandler<TService>(this IModKernel kernel)
            where TService : class
        {
            kernel.AddEventHandler<TService>();
            return kernel.Bind<TService>().ToSelf();
        }

        /// <summary>
        /// Binds and registers a service as a handler for all the events it can handle.
        /// </summary>
        /// <typeparam name="TService">The type of service being registered and bound as an event handler.</typeparam>
        /// <typeparam name="TImplementation">The implementation of the service.</typeparam>
        /// <param name="kernel">The mod's kernel.</param>
        /// <returns>The syntax that can be used to configure the event handler.</returns>
        public static IBindingInSyntax<TImplementation> BindEventHandler<TService, TImplementation>(this IModKernel kernel)
            where TService : class
            where TImplementation : class, TService
        {
            kernel.AddEventHandler<TService>();
            return kernel.Bind<TService>().To<TImplementation>();
        }

        /// <summary>
        /// Registers a service as a handler for all the events it can handle. This does not bind the service.
        /// </summary>
        /// <typeparam name="TService">The type of service being bound as an event handler. This service should be registered to your mod's kernel separately.</typeparam>
        /// <param name="kernel">The mod's kernel.</param>
        /// <returns>The mod kernel for chaining.</returns>
        public static IModKernel AddEventHandler<TService>(this IModKernel kernel)
            where TService : class
        {
            _ = kernel ?? throw new ArgumentNullException(nameof(kernel));

            var handlerTypes = new HashSet<Type>();
            var queuedTypes = new Queue<Type>();
            queuedTypes.Enqueue(typeof(TService));
            while (queuedTypes.Any())
            {
                var curType = queuedTypes.Dequeue();
                if (curType == typeof(object))
                {
                    continue;
                }

                // Add current type to set of types
                handlerTypes.Add(curType);

                // Enqueue parent type
                if (curType.BaseType != null)
                {
                    queuedTypes.Enqueue(curType.BaseType);
                }

                // Enqueue implemented interfaces
                foreach (var type in curType.GetInterfaces())
                {
                    queuedTypes.Enqueue(type);
                }
            }

            var bindHandler = typeof(BindingExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == nameof(BindingExtensions.AddEventHandler) && method.GetGenericArguments().Length == 2);

            if (bindHandler == null)
            {
                throw new Exception("An error occurred while binding an event handler: the handler could not be retrieved with reflection.");
            }

            foreach (var handlerType in handlerTypes)
            {
                bindHandler.MakeGenericMethod(typeof(TService), handlerType).Invoke(null, new object[] { kernel });
            }

            return kernel;
        }

        /// <summary>
        /// Binds a service as a handler for a particular type of event.
        /// </summary>
        /// <param name="kernel">The mod's kernel.</param>
        /// <typeparam name="TService">The type of service being bound as an event handler. This service should be injectible by your mod's kernel.</typeparam>
        /// <typeparam name="THandler">The type of event handler this service implements.</typeparam>
        /// <returns>The mod kernel for chaining.</returns>
        public static IModKernel AddEventHandler<TService, THandler>(this IModKernel kernel)
            where TService : THandler
            where THandler : class
        {
            _ = kernel ?? throw new ArgumentNullException(nameof(kernel));

            TService GetService(IContext context)
            {
                return kernel.Get<TService>(context.GetChildParameters());
            }

            kernel.GlobalKernel.Bind<ManagedEventHandler<THandler>>().ToMethod(context => new ManagedEventHandler<THandler>(GetService(context))).InTransientScope();
            return kernel;
        }

        /// <summary>
        /// Binds an event manager.
        /// </summary>
        /// <typeparam name="T">The type of the event manager.</typeparam>
        /// <param name="kernel">The mod's kernel.</param>
        /// <returns>The syntax that can be used to configure the event manager.</returns>
        public static IBindingNamedWithOrOnSyntax<T> BindEventManager<T>(this IModKernel kernel)
            where T : IEventManager
        {
            return kernel.BindEventManager(syntax => syntax.To<T>());
        }

        /// <summary>
        /// Binds an event manager.
        /// </summary>
        /// <typeparam name="T">The type of the event manager.</typeparam>
        /// <param name="kernel">The mod's kernel.</param>
        /// <param name="bindTo">A callback which binds the event manager.</param>
        /// <returns>The syntax that can be used to configure the event manager.</returns>
        public static IBindingNamedWithOrOnSyntax<T> BindEventManager<T>(this IModKernel kernel, Func<IBindingToSyntax<IEventManager>, IBindingInSyntax<T>> bindTo)
            where T : IEventManager
        {
            _ = bindTo ?? throw new ArgumentNullException(nameof(bindTo));
            _ = kernel ?? throw new ArgumentNullException(nameof(kernel));

            return kernel.GlobalKernel.Bind<IEventManager>().Forward(bindTo).InSingletonScope();
        }

        /// <summary>
        /// Exposes a service in a mod's kernel to the global kernel.
        /// </summary>
        /// <param name="kernel">The mod's kernel.</param>
        /// <typeparam name="TService">The service being exposed globally.</typeparam>
        /// <returns>The syntax that can be used to configure the service that was exposed.</returns>
        public static IBindingOnSyntax<TService> ExposeService<TService>(this IModKernel kernel)
        {
            return kernel.ExposeService<TService, TService>();
        }

        /// <summary>
        /// Exposes a service in a mod's kernel to the global kernel.
        /// </summary>
        /// <param name="kernel">The mod's kernel.</param>
        /// <typeparam name="TGlobalService">The type of service that is visible globally and will be injected by the global kernel. Generally, this would be your type's interface or base class.</typeparam>
        /// <typeparam name="TModService">The type of service that is visible within your mod. This is generally the concrete type of your service, although it could be a base class or interface as well.</typeparam>
        /// <returns>The syntax that can be used to configure the service that was exposed.</returns>
        public static IBindingOnSyntax<TModService> ExposeService<TGlobalService, TModService>(this IModKernel kernel)
            where TModService : TGlobalService
        {
            _ = kernel ?? throw new ArgumentNullException(nameof(kernel));

            return kernel.GlobalKernel.Bind<TGlobalService>().ToMethod(context => kernel.Get<TModService>(context.Parameters.ToArray())).InTransientScope();
        }

        /// <summary>
        /// Exposes a service in a mod's kernel to the global kernel.
        /// </summary>
        /// <param name="kernel">The mod's kernel.</param>
        /// <param name="globalServiceType">The type of service that is visible globally and will be injected by the global kernel. Generally, this would be your type's interface or base class.</param>
        /// <param name="modServiceType">The type of service that is visible within your mod. This is generally the concrete type of your service, although it could be a base class or interface as well.</param>
        /// <returns>The syntax that can be used to configure the service that was exposed.</returns>
        public static IBindingOnSyntax<object> ExposeService(this IModKernel kernel, Type globalServiceType, Type modServiceType)
        {
            _ = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _ = modServiceType ?? throw new ArgumentNullException(nameof(modServiceType));
            _ = globalServiceType ?? throw new ArgumentNullException(nameof(globalServiceType));

            return kernel.GlobalKernel
                .Bind(globalServiceType)
                .ToProvider(typeof(ExposedServiceProvider<>))
                .InTransientScope()
                .WithParameter(new ModKernelParameter(kernel));
        }

        private class ExposedServiceProvider<T> : Provider<T>
        {
            protected override T CreateInstance(IContext context)
            {
                var kernelParameter = context.Parameters.OfType<ModKernelParameter>().FirstOrDefault();
                if (kernelParameter == null)
                {
                    return context.ContextPreservingGet<T>();
                }

                return kernelParameter.Kernel.Get<T>(context.GetChildParameters());
            }
        }

        private class ModKernelParameter : Parameter
        {
            public IModKernel Kernel { get; }

            public ModKernelParameter(IModKernel kernel)
                : base("modKernel", kernel, false)
            {
                this.Kernel = kernel;
            }
        }
    }
}