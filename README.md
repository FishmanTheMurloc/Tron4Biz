# Tron4Biz

Tron4Biz 是一个更偏向商业逻辑的 .NET TRON 区块链 SDK，适合需要精细控制交易流程的商业应用。

## 功能特性

- **HD 钱包支持** - 助记词生成、种子派生、BIP-44 路径派生
- **更商业的客户端功能** - TRX 转账、USDT 转账(transfer)/授权(approve)/代转账(transferFrom)
- **交易步骤解耦** - 构造/签名/广播解耦，支持本地签名或外部签名
- **gRPC 通信** - 相比 HTTP 更高效
- **节点服务** - 支持 FullNode / SolidityNode 区分使用
- **依赖注入** - 方便集成

## 快速开始

### 1. 使用依赖注入配置

使用默认配置连接：

```csharp
using Tron4Biz.DependencyInjection;

var services = new ServiceCollection();
services.AddTron4Biz(builder => builder.UseMainnet());  // 或 UseShasta() / UseNile()
var serviceProvider = services.BuildServiceProvider();
```

也支持自定义配置：

```csharp
services.AddTron4Biz(builder =>
{
    builder.UseMainnet()
           .ConfigureFullNode(options =>
           {
               options.Endpoint = "http://grpc.trongrid.io:50051";
               options.ApiKey = "your-api-key";  // 可选
           })
           .ConfigureSolidityNode(options =>
           {
               options.Endpoint = "http://grpc.trongrid.io:50052";
           });
});
```

### 2. HD 钱包服务

```csharp
var hdWalletService = serviceProvider.GetRequiredService<IHDWalletService>();

// 生成助记词
string mnemonic = hdWalletService.GenerateMnemonic();
Console.WriteLine($"助记词: {mnemonic}");

// 派生种子
byte[] seed = hdWalletService.DeriveSeed(mnemonic);

// 派生地址 (BIP-44 路径: m/44'/195'/account'/0/index)
string address = hdWalletService.DeriveAddress(seed, index: 0, account: 0);
Console.WriteLine($"地址: {address}");

// 验证地址
bool isValid = hdWalletService.ValidateAddress(address);

// 派生私钥并创建密钥对象
byte[] privateKey = hdWalletService.DerivePrivateKey(seed, index: 0, account: 0);
var tronKey = TronKey.FromPrivateKey(privateKey);
Console.WriteLine($"地址: {tronKey.Address}");
```

### 3. 交易服务

**Step 1: 构造交易**

```csharp
var transactionService = serviceProvider.GetRequiredService<ITronTransactionService>();

var result = await transactionService.CreateTransactionAsync(fromAddress, toAddress, amount);
```

**Step 2: 签名**

本地私钥签名：

```csharp
// 从私钥创建密钥对象
var tronKey = new TronKey(privateKeyBytes);

// 对交易签名
var signedTx = result.Transaction.SignTransaction(tronKey);
```

外部签名：

```csharp
// Tron4Biz 输出交易原始数据 (raw_data_hex) 和 txID
var rawDataHex = result.Transaction.RawData.ToByteArray();
var txId = result.TxId;

// 将 rawDataHex 发送到外部签名服务（WalletConnect/冷钱包等）
// 外部签名服务返回签名字节 (signature)

// 用返回的签名字节附加到交易
var signedTx = result.Transaction.SignTransaction(signatureBytes);
```

**Step 3: 广播**

```csharp
var fullNodeService = serviceProvider.GetRequiredService<IFullNodeService>();

var broadcastResult = fullNodeService.BroadcastTransaction(signedTx);
```

### 4. 节点服务

```csharp
var fullNodeService = serviceProvider.GetRequiredService<IFullNodeService>();
var solidityNodeService = serviceProvider.GetRequiredService<ISolidityNodeService>();

// 获取最新区块
var block = await fullNodeService.GetNowBlockAsync();

// 查询账户信息
var account = await fullNodeService.GetAccountAsync(address);

// 查询已确认的交易 (通过 SolidityNode)
var transaction = await solidityNodeService.GetTransactionByIdAsync(txid);
```

## 项目结构

```
Tron4Biz/
├── Crypto/                          # 加密与编码
│   ├── AbiEncoder.cs                # 智能合约 ABI 编码
│   ├── Base58.cs                     # Base58Check 编码
│   ├── HDWallet.cs                   # 分层确定性钱包 (BIP-32/44)
│   ├── Mnemonic.cs                   # 助记词 (BIP-39)
│   ├── TronAddress.cs                # TRON 地址工具
│   └── TronKey.cs                    # 密钥管理
├── DependencyInjection/              # DI 扩展
│   ├── Tron4BizBuilder.cs
│   └── Tron4BizServiceCollectionExtensions.cs
├── Models/                           # 数据模型
│   ├── BlockInfo.cs
│   ├── BroadcastResult.cs
│   └── CreateTransactionResult.cs
├── Node/                             # 节点服务
│   ├── FullNodeService.cs            # FullNode 客户端
│   ├── IFullNodeService.cs
│   ├── ISolidityNodeService.cs
│   └── SolidityNodeService.cs        # SolidityNode 客户端
├── Options/                          # 配置选项
│   ├── GrpcEndpointOptions.cs
│   ├── Tron4BizOptions.cs
│   └── TronNetwork.cs
├── Proto/                            # gRPC / Protocol Buffers
│   ├── ITronClient.cs
│   ├── ITronSolidityClient.cs
│   ├── WalletGrpcClient.cs
│   ├── WalletSolidityGrpcClient.cs
│   └── api/ & core/                  # 生成的 proto 代码
├── Transactions/                     # 交易构造与签名
│   ├── TransactionExtensions.cs      # 签名扩展方法
│   ├── LocalTronTransactionService.cs # 本地交易构造
│   ├── GrpcTronTransactionService.cs  # gRPC 交易构造
│   └── ITronTransactionService.cs
├── HDWalletService.cs                 # HD 钱包服务
├── IHDWalletService.cs
├── ITronGrpcClient.cs                 # gRPC 客户端接口
└── TronGrpcClient.cs                  # gRPC 客户端实现
```

## 许可证

MIT License
