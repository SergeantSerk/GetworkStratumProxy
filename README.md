# GetworkStratumProxy
 GetworkStratumProxy (**GSP**) is a "reverse" proxy for stratum miners to mine on getWork-based servers.

 **GSP** is specifically for Stratum clients (such as TeamRedMiner, PhoenixMiner or any stratum-capable miner) to getWork-only servers. The motive behind developing this was to use third-party miners to solo mine cryptocurrencies (instead of using ethminer which supported getWork connections).

 Currently, **GSP** is tested to work with a private Ethereum network, setup with `puppeth` and mined up to a canonical chain (chain with consensus achieved). Nothing is changed with the jobs sent and received by **GSP**, only that whatever is sent and received to it is relayed to the opposite side.
 
 *TeamRedMiner pointed to `stratum+tcp://127.0.0.1:3131` which is **GSP**, **GSP** then relays getWork and submitted jobs to stratum clients and node respectively and then the node verifies submitted works by **GSP**, thus sealing the block.*
 ![image](https://user-images.githubusercontent.com/14278530/142731853-faa491f2-8014-4a4f-a81c-d4c24218509c.png)

# Install
 ## Requirements
 **GSP** is made using C#/.NET 5.0 and .NET SDK is needed to build the solution.
 
 ## Source (release)
 ```
 git clone https://github.com/SergeantSerk/GetworkStratumProxy.git
 cd GetworkStratumProxy
 dotnet build -c Release
 cd GetworkStratumProxy.ConsoleApp/bin/Release/net5.0/
 dotnet GetworkStratumProxy.ConsoleApp.dll --help
 ```
  
 ## Binaries
 Currently, no binaries are built and added to [releases](https://github.com/SergeantSerk/GetworkStratumProxy/releases) yet. As such, you have to clone this repository and build it yourself. See **Source**.

# Contribute
Pleae submit any issues when using **GSP** in order to improve it (such as uncaught exceptions, communication issues).
You can contribute to this repository as need be under [licence](LICENSE).
 
# Donations
Thank you for considering donations, they are always welcome and definitely not necessary.

- `ETH`: `0x6Ce7022F469646F2C5B8eD8c659338CB53F42863`
- `XLM`: `GD3ZLSKYOP3TKUSHBJEFZAJK53GIZQFZUXQYQENAY62QAILHOM75RH7C`
