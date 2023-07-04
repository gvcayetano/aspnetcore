// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Authorization;

public static class CascadingAuthenticationStateServiceCollectionExtensions
{
    public static IServiceCollection AddCascadingAuthenticationState(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddCascadingValue<Task<AuthenticationState>>(services =>
        {
            var authenticationStateProvider = services.GetRequiredService<AuthenticationStateProvider>();
            return new AuthenticationStateCascadingValueSource(authenticationStateProvider);
        });
    }

    private class AuthenticationStateCascadingValueSource : CascadingValueSource<Task<AuthenticationState>>, IDisposable
    {
        // This is intended to produce identical behavior to having a <CascadingAuthenticationStateProvider>
        // wrapped around the root component.

        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AuthenticationStateCascadingValueSource(AuthenticationStateProvider authenticationStateProvider)
            : base(authenticationStateProvider.GetAuthenticationStateAsync(), isFixed: false)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _authenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
        }

        private void HandleAuthenticationStateChanged(Task<AuthenticationState> newAuthStateTask)
        {
            // It's OK to discard the task because this only represents the duration of the dispatch to sync context.
            // It handles any exceptions internally by dispatching them to the renderer within the context of whichever
            // component threw when receiving the update. This is the same as how a CascadingValue doesn't get notified
            // about exceptions that happen inside the recipients of value notifications.
            _ = NotifyChangedAsync(newAuthStateTask);
        }

        public void Dispose()
        {
            _authenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
        }
    }
}
