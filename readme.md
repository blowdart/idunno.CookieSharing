# Sharing Authorization Cookies between ASP.NET 4.5 and .NET Core

A commonly expressed scenario is a desire to share logins between an existing ASP.NET 4.5 application and an ASP.NET Core application. This can be achieved by creating a login cookie which can be read by both applications.

## Caveats

As only one system is the "true" login provider, the depending application must treat the login cookie as its only source of truth. This means any adjustments to the identity in the login database will not be reflected in the other application, for example, if a user is disabled their cookie will still work on the depending application, or, if the user is added to, or removed from groups, or they edit their name, these changes will not reflect in the depending system, unless you the developer create a new login cookie.
Therefore you should either view this as a stopgap mechanism until you migrate all your applications to .NET Core, or you can replace the Cookie Middleware `Validator` in any ASP.NET Core dependent systems to talk to the login provider system via an API call to validate cookies are still accurate. This is documented in the [Cookie Middleware documentation](https://docs.asp.net/en/latest/security/authentication/cookie.html#reacting-to-back-end-changes).

## Instructions

Sharing authorization cookies between ASP.NET 4.5 and .NET Core takes a number of steps. The steps need vary based on whether you are using ASP.NET Identity or the ASP.Net Cookie Middleware without Identity. Please follow the correct instructions for your configuration.

### Adding compatibility for applications using ASP.NET Identity

The interop shim does not enabling the sharing of identity databases between applications. 
ASP.NET 4.5 uses Identity 1.0 or 2.0, ASP.NET Core uses Identity 3.0. The database schemas are different, and Identity 3.0 has some optimizations and tweaks only available in .NET Core, and the Entity Framework version available on .NET Core.
Simply put, there's no way to share an Identity database. One application, be it the ASP.NET Core application or the ASP.NET 4.5 application must be the sole login provider. This solution only allows the login provider to write a cookie compatible with both ASP.NET framework versions.



### Adding compatibility for applications using OWIN & ASP.NET Core Cookie Authentication middleware without ASP.NET Identity

1. **Install the interop package into your ASP.NET 4.5 application.**
   
   Open the nuget package manager, or the nuget console and add a reference to `Microsoft.Owin.Security.Interop`.

2. **Install the data protection extensions package into your core application.**

   Open the nuget package manager, or the nuget console and add a reference to `Microsoft.AspNetCore.DataProtection.Extensions`.

3. **Make the cookie names in both applications identical.**

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

   Now switch to your ASP.NET Core application and find the `app.UseCookieAuthentication()` call. This is normally in the `Configure()` method in `startup.cs`

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

4. **Select a common data protection repository accessible by both applications.**
   
   In these instructions we're going to use a shared directory (`C:\keyring`). 
   If your applications aren't on the same server, or can't access the same NTFS share you can use other keyring repositories.
   
   .NET Core 1.0 includes key ring repositories for shared directories and the registry. 
   
   .NET Core 1.1 will add support for Redis, Azure Blob Storage and Azure Key Vault. 
   
   You can develop your own key ring repository by implementing the `IXmlRepository` interface.
  
5. **Configure the ASP.NET Core app to use the a data protector pointing to the shared repository in the ticket formatter.**

   Return to the location where your app calls `app.UseCookieAuthentication()`.

   Add `using Microsoft.AspNetCore.DataProtection;` to the using declarations at the top of the file.

   Now add the following code before the call to `app.UseCookieAuthentication()`.

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

### Redirecting login to the other app.

1. **Update the login path in the child application to point to the login URIs on the primary application.**

   Choose which location will serve as the login server for both applications and change the `LoginPath` on the other application to point to the login server's login page.
