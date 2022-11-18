# Izzy Moonbot

Created November 27, 2021 by Dr. Romulus

Bot for Manechat Management

### Developer Onboarding

- Install Visual Studio and .NET 6. See also [Discord.Net's documentation](https://discordnet.dev/guides/getting_started/installing.html?tabs=vs-install%2Ccore2-1).
- Ask one of the existing devs for a copy of the `appsettings.json` and `appsettings.Development.json` files. These contain secrets such as Discord tokens that we don't want to store in Github. Then make sure your Discord user id is in the "DevUsers" list in these files, so your instance of Izzy will give you developer permissions.
- Ask one of the existing devs for an invite to our "Bot Testing" Discord server.
- Open Visual Studio, and ask it to open the Izzy-Moonbot.sln file in this reposiitory. After it loads, make sure there's a combo box at the top that says "Debug" (**not** "Release"). Then running the solution should launch Izzy and connect her to our Bot Testing server, after which she should start responding to Discord messages there.
