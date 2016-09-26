# Sharing Authorization Cookies between ASP.NET 4.5 and .NET Core

A commonly expressed scenario is a desire to share logins between an existing ASP.NET 4.5 application and an ASP.NET Core application. This can be achieved by creating a login cookie which can be read by both applications.

## Caveats

Sharing cookies comes with two caveats;

1. As only one system is the "true" login provider, the depending application must treat the login cookie as its only source of truth. This means any adjustments to the identity in the login database will not be reflected in the other application, for example, if a user is disabled their cookie will still work on the depending application, or, if the user is added to, or removed from groups, or they edit their name, these changes will not reflect in the depending system, unless you the developer create a new login cookie.
   Therefore you should either view cookie sharing as a stopgap mechanism until you migrate all your applications to .NET Core and sharing a single Identity 3.0 database, or you can replace the cookie middleware `Validator` in any ASP.NET Core system, or the ASP.NET 4.5 Cookie middleware `OnValidateIdentity` event to talk to the primary system via an API call to validate cookies are still accurate. 
   ASP.NET Core Validators are documented in the [Cookie Middleware documentation](https://docs.asp.net/en/latest/security/authentication/cookie.html#reacting-to-back-end-changes).

2. You cannot specify a host name in the login path, so if your applications are on different host names you cannot point Application 2's login page to Application 1's login page using cookie middleware. You could manually achieve this with redirection code in Application 1's login controller, appending a query string parameter to indicate the return URL needs to return to Application 2. You would also need to change the code in Application 1 only returns to a safe list of known hosts.

## Instructions

Sharing authorization cookies between ASP.NET 4.5 and .NET Core takes a number of steps. The steps need vary based on whether you are using ASP.NET Identity or the ASP.Net Cookie middleware without Identity. Please follow the correct instructions for your configuration.


1. **Install the interop package into your ASP.NET 4.5 application.**
   
   Open the nuget package manager, or the nuget console and add a reference to `Microsoft.Owin.Security.Interop`.

2. **Install the data protection extensions package into your ASP.NET Core application.**

   Open the nuget package manager, or the nuget console and add a reference to `Microsoft.AspNetCore.DataProtection.Extensions`.

3. **Make the cookie names in both applications identical.**

   1. **ASP.NET 4.5 applications**
   
      In your ASP.NET 4.5 application find the `app.UseCookieAuthentication()` call. This is normally in `App_Start\Startup.Auth.cs`.
   
      In the `CookieAuthenticationOptions` parameter passed to `app.UseCookieAuthentication()` set the `CookieName` property to a fixed value,
 
      ```
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
          AuthenticationType = "Cookie",
          AuthenticationMode = 
              Microsoft.Owin.Security.AuthenticationMode.Active,
          LoginPath = new PathString("/Account/Login"),
          CookieName = ".AspNet.SharedCookie"
      });
      ```

   2. **ASP.NET Core applications With Identity**

      In the ASP.NET Core application and find the `services.AddIdentity<ApplicationUser, IdentityRole>` call. This is normally in the `ConfigureServices()` method in `startup.cs`

      You must create a new parameter for cookie configuration, and pass it as the `Cookies` property on the `IdentityOptions` parameter in the call to `servicesAddIdentity<ApplicationUser, IdentityRole>()`. 
      In applications created by the ASP.NET templates this parameter does not exist.

      Change the call to `services.AddIdentity<ApplicationUser, IdentityRole>()` to add the `IdentityOptions` parameter as follows

      ```
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

   3. **ASP.NET Core applications using Cookie Middleware directly**
   
      In the ASP.NET Core application and find the `app.UseCookieAuthentication()` call. This is normally in the `Configure()` method in `startup.cs`

      In the `CookieAuthenticationOptions` parameter passed to `app.UseCookieAuthentication()` set the `CookieName` property to a fixed value,

      ```
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
   Remember the `CookieName` property must have the same value in each application.

4. **(Optionally) Change the cookie domain and HTTPS settings.**
   
   Browsers naturally share cookies between the same domain name. For example if both your sites run in subdirectories under https://contoso.com then cookies will automatically be shared.
   
   However if your sites run on subdomains a cookie issued to a subdomain will not automatically be sent by the browser to a different subdomain, for example, https://site1.contoso.com would not share cookies with https://site2.contoso.com.

   If your sites run on subdomains you can configure the issued cookies to be shared by setting the `CookieDomain` property in `CookieAuthenticationOptions` to be the parent domain. 
   For example, if you have two sites, https://site1.contoso.com and https://site2.contoso.com setting the `CookieDomain` value to `.contoso.com` will enable the cookie to be sent to both sites, or indeed any site under contoso.com. 
   Note that the domain name is prefixed with a period. This is part of the [RFC 2965](https://tools.ietf.org/html/rfc296) and [RFC 2965](https://tools.ietf.org/html/rfc6265) (section 5.1.3) standards.
   
   It is not possible to share cookies between sites on different host names, https://site1.contoso.com cannot share cookies with https://site2.fabrikam.com.

   Additionally a cookie which has the Secure flag set will not flow to an HTTP site. The ASP.NET Cookie middlewares will automatically set the secure flag when it is creating during an HTTPS request. 
   While it is possible to override this behavior using the `CookieSecure` property this is not recommended, for security if one site you want to share cookies with is on HTTPS then all sites that share the cookie should be on HTTPS.

4. **Select a common data protection repository accessible by both applications.**
   
   In these instructions we're going to use a shared directory (`C:\keyring`). 
   If your applications aren't on the same server, or can't access the same NTFS share you can use other keyring repositories.
   
   .NET Core 1.0 includes key ring repositories for shared directories and the registry. 
   
   .NET Core 1.1 will add support for Redis, Azure Blob Storage and Azure Key Vault. 
   
   You can develop your own key ring repository by implementing the `IXmlRepository` interface.
  
5. **Configure the ASP.NET Core app to use the a data protector pointing to the shared repository in the ticket formatter.**

   Return to the location where your app calls `services.AddIdentity<ApplicationUser, IdentityRole>()` or `app.UseCookieAuthentication()`.

   Add `using Microsoft.AspNetCore.DataProtection;` to the using declarations at the top of the file.

   Now add the following code before the call to `services.AddIdentity<ApplicationUser, IdentityRole>()` or `app.UseCookieAuthentication()`.

   ```
   var protectionProvider = DataProtectionProvider.Create(
       new DirectoryInfo(@"c:\keyring"));
   var dataProtector = protectionProvider.CreateProtector(
       "CookieAuthenticationMiddleware",
       "Cookie",
       "v2");
   var ticketFormat = new TicketDataFormat(dataProtector);
   ```     
   Finally update your `CookieAuthenticationOptions` to add a `TicketDataFormat` parameter, using the `ticketFormat` variable created in the code above.

   1. **ASP.NET Core applications With Identity**
      ```
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
                  CookieName = ".AspNet.SharedCookie",
                  TicketDataFormat = ticketFormat
              }
          };
       })
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();
       ```

   1. **ASP.NET Core applications using Cookie Middleware directly**
      ```
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
          AuthenticationScheme = "Cookie",
          LoginPath = new PathString("/Account/Login/"),
          AccessDeniedPath = new PathString("/Account/Forbidden/"),
          AutomaticAuthenticate = true,
          AutomaticChallenge = true,
          CookieName = ".AspNet.SharedCookie",
          TicketDataFormat = ticketFormat
      });
      ```

6. **Configure the ASP.NET 4.5 app to use the interop format, along with a fixed data protector which matches the data protector from step 5.**

   Return to the location where your app calls `app.UseCookieAuthentication()`;

   Add `using Microsoft.Owin.Security.Interop;` to the using declarations at the top of the file.

   Now add the following code before the call to `app.UseCookieAuthentication()`.

   ```
   var protectionProvider = DataProtectionProvider.Create(
       new DirectoryInfo(@"c:\keyring"));
   var dataProtector = protectionProvider.CreateProtector(
      "CookieAuthenticationMiddleware",
      "Cookie",
      "v2");
    var ticketFormat = new AspNetTicketDataFormat(new DataProtectorShim(dataProtector));
   ```
   This is the *exact same code* you added to the ASP.NET Core application.

   Finally update your `CookieAuthenticationOptions` to add a `TicketDataFormat` parameter, using the `ticketFormat` variable created in the code above.

   ```
   app.UseCookieAuthentication(new CookieAuthenticationOptions
   {
       AuthenticationType = "Cookie",
       AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
       LoginPath = new PathString("/Account/Login"),
       CookieName = ".AspNet.SharedCookie",
       TicketDataFormat = ticketFormat
   });
   ```

### Adding compatibility for applications using ASP.NET Identity

The interop shim does not enabling the sharing of identity databases between applications. 
ASP.NET 4.5 uses Identity 1.0 or 2.0, ASP.NET Core uses Identity 3.0. 
If you want to share databases you must update the ASP.NET Identity 2.0 applications to use the ASP.NET Identity 3.0 schemas.
If you are upgrading from Identity 1.0 you should migrate to Identity 2.0 first, rather than try to go directly to 3.0.

