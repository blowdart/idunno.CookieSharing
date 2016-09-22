# Sharing Identity Cookies between ASP.NET 4.5 and .NET Core

Sharing identity cookies between ASP.NET 4.5 and .NET Core takes a number of steps.

1. Install the interop package into your ASP.NET 4.5 application.
2. Install the data protection extensions package into your core application.
3. Make the cookie names the same.
3. Find a common directory for the shared data protection keyring.
4. Configure the asp.net core app to use the a fixed data protector in the ticket formatter
5. Configure the asp.net desktop app to use the interop format, along with a fixed data protector which matches the data protector from step 5.