using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Signer;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace ContractCreationTest
{
    public class Program
    {
        private const string NodeUrl = "https://sokol.poa.network/";

        private const string Address = "0x0C7Fbe6A23a69C14e1e3B1432C328Ce89999447f";
        private const string PrivateKey = "b07d5bb55af52d46418862a559e6f94cdbc56fe9dbc998c2eaf5f51be2ae8a82";
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
            var account = new Account(PrivateKey, Chain.Ropsten);
            var web3 = new Web3(account, NodeUrl)
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

            /*
             * Here's the trick. This code works perfectly with ETH nodes
             *
             * Here we first send deploy request
             * Receive 0x1 (SUCCESS) in status field and some non-zero number in contract address
             * BUT null's in transaction block hash and number
             *
             * eth_getCode with received address returns error
             */
            await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                abi: contractAbi,
                contractByteCode: contractBin,
                from: Address,
                gas: gasAmount,
                gasPrice: gasPrice,
                value: value);
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
