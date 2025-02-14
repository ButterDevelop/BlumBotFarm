# AutoBlumFarm Backend

[![Demo Video](https://img.youtube.com/vi/-OldS7lsikE/0.jpg)](https://www.youtube.com/watch?v=-OldS7lsikE)

## Overview

**AutoBlumFarm** is an automation platform built in **C#** and **ASP.NET Core** for managing a "farm" of Telegram mini-app accounts for the crypto project **Blum**. The system automates daily actions such as collecting rewards, playing drop games to earn points, activating farming tasks, and handling friend reward claims — all while mimicking human-like behavior to avoid detection.

In addition to managing its own pool of accounts, **AutoBlumFarm** allows users to purchase account slots. Once a slot is bought, users can integrate their own Telegram account and let the system automatically manage tasks (using a combination of proxy management, scheduling, and randomized actions) on their behalf.

**Other parts** of this project were also developed and can be found in the following repositories:
- AutoBlumFarm [Frontend](https://github.com/ButterDevelop/AutoBlumFarmBot)
- AutoBlumFarm [TON Smart Contract](https://github.com/ButterDevelop/AutoBlumSmartContract)
- Telegram Mini App [Auth Extractor](https://github.com/ButterDevelop/TelegramMiniAppAuthExtractor)

## Key Features

- **Automated Task Scheduling:**  
  Uses Quartz.NET to schedule tasks such as daily checks, gaming interactions, and rewards collection at randomized intervals to mimic human behavior.

- **Telegram Bot Integration:**  
  A fully integrated Telegram Bot handles user commands and admin controls. Commands include account registration, viewing stats, updating tokens, and even forcing tasks or redistributing jobs.

- **Account & Slot Management:**  
  Users can purchase slots for their accounts. The backend calculates slot prices dynamically based on the number of accounts already owned and automatically provisions new tasks and rewards.

- **Trial Mode:**
  Users can activate their trial only once and for a couple of days duration. It is completely free and made for user to test the project. The trial mode has lower farming rates.

- **Proxy & Traffic Interception:**  
  The **WalletConnectProxyServer** intercepts HTTP/HTTPS traffic, substitutes the authorization token, and routes connections through user-defined proxies. This enables seamless browser-based control of Telegram sessions on different accounts without reauthentication.

- **GUI Account Manager:**  
  An additional GUI tool (BlumBotFarm.GUIAccountManager) simplifies management of large numbers of Telegram accounts by generating commands and handling file manipulations for account startup.

## Technologies

- **Language & Frameworks:**  
  - C#  
  - ASP.NET Core (Web API)

- **Database:**  
  - MongoDB (for storing account, task, payment, and configuration data)

- **Task Scheduling:**  
  - Quartz.NET

- **Logging:**  
  - Serilog

- **Proxy Handling:**  
  - Titanium.Web.Proxy  
  - Yove.Proxy (for custom proxy types)

- **Telegram Integration:**  
  - [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) for bot functionality  
  - Custom implementations for admin and user commands

- **JavaScript Integration:**  
  - [Jering.Javascript.NodeJS](https://github.com/JeringTech/Javascript.NodeJS) for executing JavaScript payloads in game interactions (e.g., to make encryption keys and send payload to server)

- **Additional Libraries:**  
  - Newtonsoft.Json for JSON serialization  
  - Regular Expressions and System.Security.Cryptography for token management and certificate handling

## Telegram Bot Commands

### User Commands
- **`/start`**  
  Registers a new user. If a referral code is provided (e.g. `/start REFERRAL_CODE`), it automatically sets up the referral bonus.

- **`/feedback` & `/paysupport`**  
  Allows users to send feedback or payment-related support messages. The bot logs the feedback and routes it to the tech support group.

### Admin Commands
*(Only accessible by approved admin chat IDs)*

- **`/addaccount`**  
  Adds a new Telegram account to the system. The command supports multiple formats:
  - `/addaccount <username> <refreshToken> <userId> [<proxy>]`  
  - `/addaccount <username> <accessToken> <refreshToken> <timezoneOffset> <userId> [<proxy>]`

- **`/stats`**  
  Displays overall system statistics including account counts, daily rewards taken, total balances, tickets, earnings, and referral data.

- **`/info`**  
  Retrieves detailed information about a specific account by username.

- **`/proxy`**  
  Updates or removes the proxy configuration for a specified account.

- **`/forcedailyjob` & `/forcedailyjobforeveryone`**  
  Forces an unscheduled daily task (DailyCheckJob) either for a specific account or for all accounts that have pending tickets.

- **`/jobsinfo`**  
  Provides details on the next scheduled job times for accounts.

- **`/newsletter`**  
  Forwards a message (specified by channel name and post ID) as a newsletter to all users.

- **`/refreshtoken` & `/providertoken`**  
  Updates the refresh token or provider token for an account.

- **`/redistributetasks`**  
  Deletes all scheduled tasks and immediately executes the main scheduler job to redistribute tasks.

- **`/updateusersinfo`**  
  Triggers an immediate update of user information from the external game API.

- **`/userusdbalance`**  
  Gets or sets the USD balance for a user (with security hash validation).

- **`/config`**  
  Retrieves or updates backend configuration settings (e.g. enabling task execution or adjusting chance rates).

- **`/authcheck`**  
  Forces an authentication check for a specified account, reauthenticating if necessary.

- **`/restartapp`**  
  Forces a restart of the backend application.

*Note:* The commands above are processed by both the user and admin Telegram bot implementations in the project. Refer to the source code in the `BlumBotFarm.TelegramBot` and `BlumBotFarm.AdminTelegramBot` directories for full command details and additional options.

## Project Structure

Below is an abbreviated overview of the repository structure:
```
AutoBlumFarm/
├── AutoBlumFarmServer/            # ASP.NET Core backend API project
│ ├── Controllers/                 # API endpoints (e.g. PurchaseController, UserController, TelegramAuthController, etc.)
│ ├── DTO/                         # Data transfer objects for requests and responses
│ ├── Helpers/                     # Utility classes (e.g. HTTPController, ProxySellerAPIHelper)
│ ├── Model/                       # Domain models (Account, Task, Earning, etc.)
│ ├── Database/                    # MongoDB repository implementations
│ ├── SwaggerApiResponses/         # Swagger response examples for API documentation
│ └── Program.cs                   # Application entry point
├── BlumBotFarm.Core/              # Core functionality for game API and proxy clients
├── BlumBotFarm.Database/          # Database repository interfaces and implementations
├── BlumBotFarm.Scheduler/         # Quartz-based job scheduler and job definitions
├── BlumBotFarm.TelegramBot/       # Telegram Bot implementations (user & admin)
├── BlumBotFarm.GUIAccountManager/ # Windows Forms GUI tool for account management
└── WalletConnectProxyServer/      # Proxy server project for intercepting and modifying traffic
```

## Video Demonstration

Click the image above (or [this link](https://www.youtube.com/watch?v=-OldS7lsikE)) to watch a demo video showcasing how **AutoBlumFarm** works in action.

## Contributing

Contributions, improvements, and bug fixes are welcome. Please feel free to fork the repository and open a pull request with your proposed changes. Before contributing, ensure that you adhere to our coding conventions and review the project’s architecture to understand its various components (e.g., API endpoints, scheduler jobs, Telegram bot commands, etc.).

## License

[MIT](LICENSE)

---

*This backend is designed to work in tandem with a separate frontend repository. For frontend instructions and documentation, please refer to the corresponding repository.*

**Happy farming!**
