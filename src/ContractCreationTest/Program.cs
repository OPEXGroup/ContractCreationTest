using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace ContractCreationTest
{
    public class Program
    {
        // Change this to start using mainnet/testnet
        private const bool UseTestNet = true;

        private const string TestNetNodeUrl = "https://sokol.poa.network/";

        private const string MainNetNodeUrl = "https://core.poa.network/";
        /**
         * NB: Ids!
         */
        private const Chain SokolNetworkId = (Chain) 77;
        // ReSharper disable once UnusedMember.Local
        private const Chain PoaMainNetNetworkId = (Chain) 99;

        private const string Address = "0x7D072707f4869cE64c7d3Bf5a6240Acff5277767";
        private const string PrivateKey = "95eacdba1740c625cf7f418f76db431ea7feb9d61860ac8ffff005ad9a9ef3f0";
        private const string ContractOutputFolder = "ContractOutput";

        private static async Task Main(string[] args)
        {
            try
            {
                CompileContract(args);

                await DeployContractAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            } 
        }

        private static void CompileContract(string[] args)
        {
            var solcPath = Path.GetFullPath(GetSolcPath(args));
            if (! File.Exists(solcPath))
                throw new Exception($"solc in unavailable ({solcPath})");
            Log($"Using compiler {solcPath}");
            var sourcePath = Path.GetFullPath(GetContractSourcePath());
            Log($"Using contract {sourcePath}");

            var compilerArgs = $"--bin --abi --overwrite --optimize -o {ContractOutputFolder} {sourcePath}";
            Log($"Running compiler with args {compilerArgs}");

            var process = Process.Start(solcPath, compilerArgs);
            if (process == null)
            {
                throw new Exception("Failed to start compiler");
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new Exception("Failed to compile contract");

            Log("Contract compiled");
        }

        private static async Task DeployContractAsync()
        {
            var account = new Account(PrivateKey, UseTestNet ? SokolNetworkId : PoaMainNetNetworkId);
            var web3 = new Web3(account, UseTestNet ? TestNetNodeUrl : MainNetNodeUrl)
            {
                Client =
                {
                    // Log all requests and responses
                    OverridingRequestInterceptor = new LoggingRequestInterceptor()
                }
            };

            var contractAbi = File.ReadAllText($"{ContractOutputFolder}/Test.abi");
            var contractBin = File.ReadAllText($"{ContractOutputFolder}/Test.bin");

            // 20 GWei. Feel free to change
            var gasPrice = new HexBigInteger(new BigInteger(20_000_000_000));
            // Contract creation transactions have zero value
            var value = new HexBigInteger(new BigInteger(0));

            var gasAmount = await web3.Eth.DeployContract.EstimateGasAsync(contractAbi, contractBin, Address);
            Log($"Estimated gas amount for deploy: {gasAmount.Value}");

            var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                abi: contractAbi,
                contractByteCode: contractBin,
                from: Address,
                gas: gasAmount,
                gasPrice: gasPrice,
                value: value);
            var contractAddress = receipt.ContractAddress;
            Log($"Contract deployed at address {contractAddress}");

            var contract = web3.Eth.GetContract(contractAbi, contractAddress);
            var ownerFunction = contract.GetFunction("owner");

            var ownerValue = await ownerFunction.CallAsync<string>();
            Log($"Owner is {ownerValue}. Correct: {string.Equals(ownerValue, Address, StringComparison.OrdinalIgnoreCase)}");
        }

        private static string GetSolcPath(string[] args)
        {
            // We can pass solc executable path as first arg
            if (args.Length > 1)
            {
                var path = args[1];
                if (File.Exists(path))
                    return path;
            }

            // MSVS hack (be compatible with 'netcoreapp2.1' in build path)
            const string firstTry = "../tools/win32/solc.exe";
            if (File.Exists(firstTry))
                return firstTry;

            return "../" + firstTry;
        }

        private static string GetContractSourcePath()
        {
            // MSVS hack (be compatible with 'netcoreapp2.1' in build path)
            const string firstTry = "../contract/Test.sol";
            if (File.Exists(firstTry))
                return firstTry;

            return "../" + firstTry;
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
