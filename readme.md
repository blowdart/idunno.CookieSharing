# Sharing Authorization Cookies between ASP.NET 4.5 and .NET Core

A commonly expressed scenario is a desire to share logins between an existing ASP.NET 4.5 application and an ASP.NET Core application. This can be achieved by creating a login cookie which can be read by both applications.

## Instructions

Sharing authorization cookies between ASP.NET 4.5 and .NET Core takes a number of steps. The steps need vary based on whether you are using ASP.NET Identity or the ASP.Net Cookie middleware without Identity. Please follow the correct instructions for your configuration.


1. **Install the interop packages into your applications.**
   
   1. **ASP.NET 4.5**

      Open the nuget package manager, or the nuget console and add a reference to `Microsoft.Owin.Security.Interop`.

   2. **ASP.NET Core**

      Open the nuget package manager, or the nuget console and add a reference to `Microsoft.AspNetCore.DataProtection.Extensions`.

2. **Make the CookieName and AuthenticationType/AuthenticationScheme in both applications identical.**

   1. **ASP.NET 4.5**
   
      In your ASP.NET 4.5 application find the `app.UseCookieAuthentication()` call. This is normally in `App_Start\Startup.Auth.cs`.
   
      In the `CookieAuthenticationOptions` parameter passed to `app.UseCookieAuthentication()` set the `CookieName` property to a value that will be shared between all applications and also set the `AuthenticationType` to a value that will be shared between applications,
 
      ```C#
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
          AuthenticationType = "Cookie",
          AuthenticationMode = 
              Microsoft.Owin.Security.AuthenticationMode.Active,
          LoginPath = new PathString("/Account/Login"),
          CookieName = ".AspNet.SharedCookie"
      });
      ```

   2. **ASP.NET Core with ASP.NET Identity**

      In the ASP.NET Core application and find the `services.AddIdentity<ApplicationUser, IdentityRole>` call. This is normally in the `ConfigureServices()` method in `startup.cs`

      You must create a new parameter for cookie configuration, and pass it as the `Cookies` property on the `IdentityOptions` parameter in the call to `servicesAddIdentity<ApplicationUser, IdentityRole>()`. *In applications created by the ASP.NET templates this parameter does not exist - you will need to add it.*

      Change the call to `services.AddIdentity<ApplicationUser, IdentityRole>()` to add the `IdentityOptions` parameter and set the `CookieName` property to be the same value you set in the ASP.NET 4.5 application and set the `AuthenticationScheme` property to be the same value you used for `AuthenticationType` in the ASP.NET 4.5 application

      ```C#
      services.AddIdentity<ApplicationUser, IdentityRole>(options =>
      {
          options.Cookies = new Microsoft.AspNetCore.Identity.IdentityCookieOptions
          {
              ApplicationCookie = new CookieAuthenticationOptions
              {
                  AuthenticationScheme = "Cookie",
                  LoginPath = new PathString("/Account/Login/"),
                  AccessDeniedPath = new PathString("/Account/Forbidden/"),
                  AutomaticAuthenticate = true,
                  AutomaticChallenge = true,
                  CookieName = ".AspNet.SharedCookie"
                             
              };
          })
          .AddEntityFrameworkStores<ApplicationDbContext>()
          .AddDefaultTokenProviders();
      ```

   3. **ASP.NET Core using Cookie Middleware directly**
   
      In the ASP.NET Core application and find the `app.UseCookieAuthentication()` call. This is normally in the `Configure()` method in `startup.cs`

      In the `CookieAuthenticationOptions` parameter passed to `app.UseCookieAuthentication()` 
      set the `CookieName` property to be the same value you set in the ASP.NET 4.5 application and
      set the `AuthenticationScheme` property to be the same value you used for `AuthenticationType` in the ASP.NET 4.5 application

      ```C#
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
          AuthenticationScheme = "Cookie",
          LoginPath = new PathString("/Account/Login/"),
          AccessDeniedPath = new PathString("/Account/Forbidden/"),
          AutomaticAuthenticate = true,
          AutomaticChallenge = true,
          CookieName = ".AspNet.SharedCookie",
      });
      ```
      
   *Remember the `CookieName` property must have the same value in each application, and the `AuthenticationType` (ASP.NET 4.5) and `AuthenticationScheme` (ASP.NET Core) properties must have the same value in each application.*

   4. **(Optionally) Change the cookie domain and HTTPS settings.**
   
      Browsers naturally share cookies between the same domain name. For example if both your sites run in subdirectories under https://contoso.com then cookies will automatically be shared.
   
      However if your sites run on subdomains a cookie issued to a subdomain will not automatically be sent by the browser to a different subdomain, for example, https://site1.contoso.com would not share cookies with https://site2.contoso.com.

      If your sites run on subdomains you can configure the issued cookies to be shared by setting the `CookieDomain` property in `CookieAuthenticationOptions` to be the parent domain. 
      
      For example, if you have two sites, https://site1.contoso.com and https://site2.contoso.com setting the `CookieDomain` value to `.contoso.com` will enable the cookie to be sent to both sites, or indeed any site under contoso.com. 
      Note that the domain name is prefixed with a period. This is part of the [RFC 2965](https://tools.ietf.org/html/rfc296) and [RFC 2965](https://tools.ietf.org/html/rfc6265) (section 5.1.3) standards.
   
      It is not possible to share cookies between sites on different host names, https://site1.contoso.com cannot share cookies with https://site2.fabrikam.com.

      Additionally a cookie which has the Secure flag set will not flow to an HTTP site. The ASP.NET Cookie middlewares will automatically set the secure flag when it is creating during an HTTPS request. While it is possible to override this behavior using the `CookieSecure` property *this is not recommended* due to security concerns. If one site you want to share cookies with is on HTTPS then all sites that share the cookie should be on HTTPS.

3. **Select a common data protection repository location accessible by both applications.**
   
   In these instructions we're going to use a shared directory (`C:\keyring`). 
   If your applications aren't on the same server, or can't access the same NTFS share you can use other keyring repositories.
   
   .NET Core 1.0 includes key ring repositories for shared directories and the registry. 
   
   .NET Core 1.1 will add support for Redis, Azure Blob Storage and Azure Key Vault. 
   
   You can develop your own key ring repository by implementing the `IXmlRepository` interface.
  
4. **Configure your applications to use the same cookie format**

   1. **ASP.NET 4.5**

      Return to the location where your app calls `app.UseCookieAuthentication()`;

      Add `using Microsoft.Owin.Security.Interop;` to the using declarations at the top of the file.

      Now add the following code before the call to `app.UseCookieAuthentication()`.

      ```C#
      var protectionProvider = DataProtectionProvider.Create(
          new DirectoryInfo(@"c:\keyring"));
      var dataProtector = protectionProvider.CreateProtector(
         "CookieAuthenticationMiddleware",
         "Cookie",
         "v2");
      var ticketFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector));
      ```

      Finally update your `CookieAuthenticationOptions` to add a `TicketDataFormat` parameter, using the `ticketFormat` variable created in the code above.

      ```C#
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
          AuthenticationType = "Cookie",
          CookieName = ".AspNet.SharedCookie",
          TicketDataFormat = ticketFormat
      });
      ```

    2. **ASP.NET Core**
       Return to the location where your app calls `services.AddIdentity<ApplicationUser, IdentityRole>()` or `app.UseCookieAuthentication()`.

       Add `using Microsoft.AspNetCore.DataProtection;` to the using declarations at the top of the file.

       Now add the following code before the call to `services.AddIdentity<ApplicationUser, IdentityRole>()` or `app.UseCookieAuthentication()`.

       ```C#
       var protectionProvider = DataProtectionProvider.Create(
           new DirectoryInfo(@"c:\keyring"));
       var dataProtector = protectionProvider.CreateProtector(
           "CookieAuthenticationMiddleware",
           "Cookie",
           "v2");
       var ticketFormat = new TicketDataFormat(dataProtector);
       ```     
   
       Finally update your `CookieAuthenticationOptions` to add a `TicketDataFormat` parameter, using the `ticketFormat` variable created in the code above.

       1. **ASP.NET Core with ASP.NET Identity**
          ```C#
          services.AddIdentity<ApplicationUser, IdentityRole>(options =>
          {
              options.Cookies = new Microsoft.AspNetCore.Identity.IdentityCookieOptions
              {
                  options.Cookies.AuthenticationScheme = "Cookie",
                  options.Cookies.ApplicationCookie.CookieName =
                      ".AspNet.SharedCookie",
                  options.Cookies.ApplicationCookie.TicketDataFormat = ticketFormat;
              };
           })
           .AddEntityFrameworkStores<ApplicationDbContext>()
           .AddDefaultTokenProviders();
           ```

        2. **ASP.NET Core using Cookie Middleware directly**
           ```C#
           app.UseCookieAuthentication(new CookieAuthenticationOptions
           {
               AuthenticationScheme = "Cookie",
               CookieName = ".AspNet.SharedCookie",
               TicketDataFormat = ticketFormat
           });
           ```

### Adding compatibility for applications using ASP.NET Identity

The interop shim does not enabling the sharing of identity databases between applications. ASP.NET 4.5 uses Identity 1.0 or 2.0, ASP.NET Core uses Identity 3.0. If you want to share databases you must update the ASP.NET Identity 2.0 applications to use the ASP.NET Identity 3.0 schemas. If you are upgrading from Identity 1.0 you should migrate to Identity 2.0 first, rather than try to go directly to 3.0.
