﻿using System;
using Owin;

namespace OAuth.AuthorizationServer.Middleware
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Create a new instance of DummyAuthenticationMiddleware.
        /// </summary>
        public static IAppBuilder UseDummyAuthentication(this IAppBuilder app, DummyAuthenticationOptions options)
        {
            if (null == app) throw new ArgumentNullException("app");
            if (null == options) throw new ArgumentNullException("options");
            return app.Use(typeof (DummyAuthenticationMiddleware), app, options);
        }
    }
}
