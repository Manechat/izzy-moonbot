# Izzy Moonbot

![dotnet](https://github.com/Manechat/izzy-moonbot/actions/workflows/dotnet.yml/badge.svg)
![docker-build-deploy](https://github.com/Manechat/izzy-moonbot/actions/workflows/docker-build-deploy.yml/badge.svg)
![package-prune](https://github.com/Manechat/izzy-moonbot/actions/workflows/package-prune.yml/badge.svg)

Created November 27, 2021 by Dr. Romulus

Bot for Manechat Management

### Manechat Moderator Onboarding

Get started by posting `.help` and/or `.config` in one of the private bots-* channels. From there you can explore Izzy's self-documentation for all commands and configuration items. You don't need to read all of them, but browsing them should give you a pretty good idea what she can do.

### Developer Onboarding

- Create a Github account if you don't already have one
- Install:
  - Visual Studio Community
    - Include the ASP.NET development workload
  - .NET 7
  - WSL (Windows Subsystem for Linux)
    - Install Ubuntu as default distro for best ease of use
  - Docker Desktop
    - After installation you likely need to go into Settings (the cog wheel icon) -> Resources -> WSL Integration -> "Enable integration with additional distros" and enable "Ubuntu"
- Ask one of the existing devs for a copy of `Izzy-Moonbot/Docker Compose/Dev/appsettings.json`. This contains secrets such as Discord tokens and database credentials that we don't want to store in GitHub.
- Add your Discord user id to the "DevUsers" list in `appsettings.json`, so your instance of Izzy will give you developer permissions.
- Ask one of the existing devs to add you to the "DevUsers" list in the `appsettings.json` file on Izzy's production environment.
- Ask one of the existing devs for an invite to our "Bot Testing" Discord server.
- Launch a WSL terminal, `cd` over to `<your local repo clone>/Izzy-Moonbot/Docker Compose/Dev`, then running `docker compose up` should launch Izzy and connect her to our Bot Testing server, after which she should start responding to Discord messages there.
  - Use `docker compose up -d` if you don't want Docker taking over control of your terminal.
- Open Visual Studio, and ask it to open the Izzy-Moonbot.sln file in this repository. This is what you'll use to edit the code and run unit tests. Make sure Run All Tests succeeds for you.
- Practice performing all the steps in [the manual testing script](https://github.com/Manechat/izzy-moonbot/blob/mane/ManualTestingScript.md), since we recommend following that on any significant or risky changes to Izzy.
- Ask one of the existing devs to add your SSH public key to the `~/.ssh/authorized_keys` file on Izzy's production host machine. If you don't have SSH keys already, there are many ways to generate a keypair. Feel free to ask for help generating a keypair if you need it. Using `ed25519` keys instead of `rsa` keys is highly recommended.
  - If you choose to associate a passphrase with your ssh keys, keep in mind you'll need to provide it on every single ssh operation (and [if you want to sign your `git` commits](https://docs.github.com/en/authentication/managing-commit-signature-verification/telling-git-about-your-signing-key#telling-git-about-your-ssh-key), also on every git operation)
- To connect to Izzy's production host, ask an existing dev for the user & hostname.

### Releasing and Monitoring Izzy

Once all the changes you want to release have been merged onto the `mane` branch, wait for CI to complete building the Docker images. Image builds are handled by GitHub Actions and are automatically uploaded to GitHub Container Registry. If you have multiple PRs to release/merge into `mane`, give each CI a few minutes to run before releasing the next one to avoid CI run and package upload conflicts.

Once CI is complete and images are built, the below is the release process for now, until further changes are implemented.
- SSH into Izzy's prod host
- `cd izzy-compose`
- `docker compose up -d --force-recreate --no-deps izzy`
- This will trigger Docker to bring down the old container, pull the latest image, and spin up a new container with the latest image build.
- You may use `docker logs izzymoonbot-izzy-1 -f` to monitor Izzy's logs, or access them via the Portainer UI. Logs do not persist between container recreations.
