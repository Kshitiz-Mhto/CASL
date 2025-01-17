﻿// <copyright file="ExtensionMethods.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace CASL
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using SimpleInjector;
    using SimpleInjector.Diagnostics;

    /// <summary>
    /// Provides helper methods for use throughout the application.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        ///     Registers that a new instance of <typeparamref name="TImplementation"/> will be returned every time
        ///     a <typeparamref name="TService"/> is requested (transient).
        /// </summary>
        /// <typeparam name="TService">The interface or base type that can be used to retrieve the instances.</typeparam>
        /// <typeparam name="TImplementation">The concrete type that will be registered.</typeparam>
        /// <param name="container">The container that the registration applies to.</param>
        /// <param name="suppressDisposal"><see langword="true"/> to ignore dispose warnings if the original code invokes dispose.</param>
        /// <remarks>
        ///     This method uses the container's LifestyleSelectionBehavior to select the exact
        ///     lifestyle for the specified type. By default this will be Transient.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when one of the arguments is a null reference.</exception>
        /// <exception cref="InvalidOperationException">Thrown when this container instance is locked and can not be altered.</exception>
        [ExcludeFromCodeCoverage]
        public static void Register<TService, TImplementation>(this Container container, bool suppressDisposal = false)
            where TService : class
            where TImplementation : class, TService
        {
            container.Register<TService, TImplementation>();

            if (suppressDisposal)
            {
                SuppressDisposableTransientWarning<TService>(container);
            }
        }

        /// <summary>
        ///     Conditionally registers that a new instance of <typeparamref name="TImplementation"/> will be returned
        ///     every time a <typeparamref name="TService"/> is requested (transient) and where the supplied predicate
        ///     returns true. The predicate will only be evaluated a finite number of times;
        ///     the predicate is unsuited for making decisions based on runtime conditions.
        /// </summary>
        /// <typeparam name="TService">The interface or base type that can be used to retrieve the instances.</typeparam>
        /// <typeparam name="TImplementation">The concrete type that will be registered.</typeparam>
        /// <param name="container">The container that the registration applies to.</param>
        /// <param name="predicate">
        ///     The predicate that determines whether the <typeparamref name="TImplementation"/> can be applied for
        ///     the requested service type. This predicate can be used to build a fallback mechanism
        ///     where multiple registrations for the same service type are made. Note that the
        ///     predicate will be called a finite number of times and its result will be cached
        ///     for the lifetime of the container. It can't be used for selecting a type based
        ///     on runtime conditions.
        /// </param>
        /// <param name="suppressDisposal"><see langword="true"/> to ignore dispose warnings if the original code invokes dispose.</param>
        /// <remarks>
        ///     This method uses the container's LifestyleSelectionBehavior to select the exact
        ///     lifestyle for the specified type. By default this will be Transient.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when one of the arguments is a null reference.</exception>
        /// <exception cref="InvalidOperationException">Thrown when this container instance is locked and can not be altered.</exception>
        [ExcludeFromCodeCoverage]
        public static void RegisterConditional<TService, TImplementation>(this Container container, Predicate<PredicateContext> predicate, bool suppressDisposal = false)
            where TService : class
            where TImplementation : class, TService
        {
            container.RegisterConditional<TService, TImplementation>(predicate);

            if (suppressDisposal)
            {
                SuppressDisposableTransientWarning<TService>(container);
            }
        }

        /// <summary>
        /// Suppresses SimpleInjector diagnostic warnings related to disposing of objects when they
        /// inherit from <see cref="IDisposable"/>.
        /// </summary>
        /// <typeparam name="T">The type to suppress against.</typeparam>
        /// <param name="container">The container that the suppression applies to.</param>
        [ExcludeFromCodeCoverage]
        public static void SuppressDisposableTransientWarning<T>(this Container container)
        {
            var registration = container.GetRegistration(typeof(T))?.Registration;
            registration?.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "Disposing of objects to be disposed of manually by the library.");
        }

        /// <summary>
        /// Returns a list of strings based on the space delimited string pointed to by <paramref name="strPtr"/>.
        /// </summary>
        /// <param name="strPtr">A pointer to the string of unknown length.</param>
        /// <returns>A list of strings.</returns>
        /// <remarks>
        ///     Checks for spaces between sections of strings and splits each string section
        ///     up and returns each section as part of a list of strings.
        /// </remarks>
        public static string[] ToStrings(this nint strPtr)
        {
            if (strPtr == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();

            var currentPos = strPtr;

            while (true)
            {
                var currentString = Marshal.PtrToStringAnsi(currentPos);

                if (string.IsNullOrEmpty(currentString))
                {
                    break;
                }

                result.Add(currentString);
                currentPos += currentString.Length + 1;
            }

            return result.ToArray();
        }
    }
}
