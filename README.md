# Izzy Moonbot

Created November 27, 2021 by Dr. Romulus

Bot for Manechat Management

### Developer Onboarding

- Install Visual Studio and .NET 6. See also [Discord.Net's documentation](https://discordnet.dev/guides/getting_started/installing.html?tabs=vs-install%2Ccore2-1).
- Ask one of the existing devs for a copy of the `appsettings.json` and `appsettings.Development.json` files (or skip ahead to the `ssh` steps below, then `scp` it yourself). These contain secrets such as Discord tokens that we don't want to store in Github.
- Add your Discord user id to the "DevUsers" list in `appsettings.json` and `appsettings.Development.json`, so your instance of Izzy will give you developer permissions.
- Ask one of the existing devs for an invite to our "Bot Testing" Discord server.
- Open Visual Studio, and ask it to open the Izzy-Moonbot.sln file in this repository. After it loads, make sure there's a combo box at the top that says "Debug" (**not** "Release"). Then running the solution should launch Izzy and connect her to our Bot Testing server, after which she should start responding to Discord messages there.
- Practice performing all the steps in [the manual testing script](https://github.com/Manechat/izzy-moonbot/blob/mane/ManualTestingScript.md), since we recommend following that on any significant or risky changes to Izzy.
- Ask one of the existing devs to add your ssh public key to the `~/.ssh/authorized_keys` file on Izzy's production host machine. If you don't have keys and/or terminals for ssh, then [install git for Windows](https://gitforwindows.org/), open the "git bash" shell it comes with, and run `ssh-keygen -t ed25519` inside git bash. This will place a private key in `~/.ssh/id_ed25519` and a public key in `~/.ssh/id_ed25519.pub`.
  - If you choose to associate a passphrase with your ssh keys, keep in mind you'll need to provide it on every single ssh operation (and [if you want to sign your `git` commits](https://docs.github.com/en/authentication/managing-commit-signature-verification/telling-git-about-your-signing-key#telling-git-about-your-ssh-key), also on every git operation), so you'll probably want to add something like ``eval `ssh-agent -s` `` and `ssh-add ~/.ssh/id_ed25519` to your `~/.bashrc` so that `ssh-agent` will ask for the passphrase only once per session then automagically use it on every relevant command.
- To connect to Izzy's production host, add the following to `~/.ssh/config`:
```
Host izzy
    Port 22
    HostName 152.67.70.34
    User ubuntu
    IdentityFile ~/.ssh/id_ed25519
```
then run `ssh izzy`.
- On the production host machine, look at the files in `~/izzy-moonbot/botsettings/` to see Izzy's prod configuration/state/data, and use `journalctl -u izzy-moonbot.service -f` to monitor Izzy's logs.

### Releasing and Monitoring Izzy

Once all the changes you want to release have been merged onto the `mane` branch, and you've done `git pull origin mane` or whatever your preferred command is to update your local repo:

- Open two terminals (I use git bash). Run `ssh izzy` on one of them, and `cd` to your local clone of this `izzy-moonbot` repo on the other.
- On the prod host: run `cp -r ~/izzy-moonbot/botsettings/* ~/izzy-backup/config` to back up all of Izzy's configuration files (optionally use `ls -l ~/izzy-backup/config` to double-check the last modified timestamps of those backups)
- In your local clone: run `dotnet build Izzy-Moonbot/Izzy-Moonbot.csproj -c Release -r linux-x64 --self-contained` to build Izzy
- On the prod host: run `sudo service izzy-moonbot stop` to disable Izzy
- In your local clone: run `scp -r Izzy-Moonbot/bin/Release/net6.0/linux-x64/* izzy:~/izzy-moonbot` to copy the build artifacts to the production host
- On the prod host: run `sudo service izzy-moonbot start && journalctl -u izzy-moonbot.service -f` to start Izzy and monitor her startup logs
