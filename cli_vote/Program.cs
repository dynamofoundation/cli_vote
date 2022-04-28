﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace cli_vote
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("*******************************************");
            Console.WriteLine("Dynamo coin command line voting utiity");
            Console.WriteLine("This utility will cast a vote for all coins");
            Console.WriteLine("held in an HD wallet hosted by a fullnode or");
            Console.WriteLine("QT server.");
            Console.WriteLine("*******************************************");


            /*

            Console.Write("Enter rpc server name or address: ");
            Global.server = Console.ReadLine();

            Console.Write("Enter rpc server name port: ");
            Global.port = Console.ReadLine();

            Console.Write("Enter rpc server username: ");
            Global.username = Console.ReadLine();

            Console.Write("Enter rpc server password: ");
            Global.password = Console.ReadLine();

            Console.Write("Enter wallet name: ");
            Global.wallet = Console.ReadLine();

            Console.Write("Enter wallet password (or blank of none): ");
            Global.walletpass = Console.ReadLine();

            Console.Write("Enter minimum address balance to vote (e.g. 0.1): ");
            Global.min_balance = Console.ReadLine();

            Console.Write("Enter TXID of voting proposal: ");
            Global.nft_hash = Console.ReadLine();

            Console.Write("Enter your vote (01 = YES, 02 = NO, or number for multiple choice votes): ");
            Global.vote = Console.ReadLine();

            */

            Global.server = "192.168.4.48";
            Global.port = "6433";
            Global.username = "user";
            Global.password = "123456";
            Global.wallet = "testvote";
            Global.walletpass = "";
            Global.txid_proposal = "0000000000000000000000000000000000000000000000000000000000000000";
            Global.vote = "60";
            Global.min_balance = "0.1";


            Dictionary<string, decimal> balances = new Dictionary<string, decimal>();
            Dictionary<string, string> utxo = new Dictionary<string, string>();

            string command = "{ \"id\": 0, \"method\" : \"loadwallet\", \"params\" : [ \"" + Global.wallet + "\" ] }";
            Utility.rpcExec(command);

            command = "{ \"id\": 0, \"method\" : \"listunspent\", \"params\" : [ ] }";
            string result = Utility.rpcExec(command);

            dynamic dResult = JObject.Parse(result);

            decimal total = 0;

            foreach (dynamic o in dResult.result)
            {
                string txid = o.txid;
                UInt32 vout = o.vout;
                string address = o.address;
                decimal amount = o.amount;

                if (balances.ContainsKey(address))
                {
                    decimal i = balances[address];
                    i += amount;
                    balances[address] = i;
                }
                else
                {
                    balances.Add(address, amount);
                    utxo.Add(address, txid + "," + vout + "," + amount);
                }

                total += amount;

                Utility.log(txid + " " + vout + " " + address + " " + amount);
            }

            Utility.log("Summary of addresses:");
            foreach (string addr in balances.Keys)
                Utility.log(addr + " " + balances[addr]);

            Console.WriteLine("Total coins found: " + total);
            Console.WriteLine("*******************************************");
            Console.WriteLine("Please verify and confirm the following:");
            Console.WriteLine("NFT HASH TO VOTE ON: " + Global.txid_proposal);
            Console.WriteLine("VOTE: " + Global.vote);
            Console.WriteLine("Ignore address balance less than: " + Global.min_balance);
            Console.WriteLine("*******************************************");
            Console.Write(@"Type ""yes"" to confirm and proceed:");
            string confirm = Console.ReadLine();
            if (confirm == "yes")
            {
                decimal dMinBal = Convert.ToDecimal(Global.min_balance);
                foreach (string addr in balances.Keys)
                    if (balances[addr] > dMinBal)
                    {
                        string message = "020000" + Global.vote + Global.txid_proposal + Utility.ByteArrayToHexString(Encoding.ASCII.GetBytes(addr));
                        command = "{ \"id\": 0, \"method\" : \"dumpprivkey\", \"params\" : [ \"" + addr + "\" ] }";
                        dynamic privkey = JObject.Parse(Utility.rpcExec(command));
                        string strPrivkey = privkey.result;

                        command = "{ \"id\": 0, \"method\" : \"signmessagewithprivkey\", \"params\" : [ \"" + strPrivkey + "\", \"" + message + "\" ] }";
                        dynamic signature = JObject.Parse(Utility.rpcExec(command));
                        string strSig = signature.result;
                        string hexSig = Utility.ByteArrayToHexString(Encoding.ASCII.GetBytes(strSig));

                        message = message + hexSig;

                        int len = message.Length / 2;
                        string hexLen = len.ToString("X");

                        string txdata = "6a" + hexLen + message;

                        string[] strUtxo = utxo[addr].Split(",");

                        string input = "{\"txid\":\"" + strUtxo[0]  + "\",\"vout\":" + strUtxo[1] + "}";
                        decimal utxoAmt = Convert.ToDecimal(strUtxo[2]);
                        decimal change = utxoAmt - 0.0001m;

                        string output1 = "{\"" + addr + "\":" + change + "}";
                        string output2 = "{\"data\":\"" + txdata + "\"}";

                        string txparams = "[ [" + input + "], [" + output1 + "," + output2 + "]]";

                        command = "{ \"id\": 0, \"method\" : \"createrawtransaction\", \"params\" : " + txparams + " }";
                        dynamic dTransaction = JObject.Parse(Utility.rpcExec(command));

                        command = "{ \"id\": 0, \"method\" : \"signrawtransactionwithkey\", \"params\" : [\"" + dTransaction.result + "\", [\"" + strPrivkey + "\"] ]}";
                        dynamic dSignedTransaction = JObject.Parse(Utility.rpcExec(command));

                        command = "{ \"id\": 0, \"method\" : \"sendrawtransaction\", \"params\" : [\"" + dSignedTransaction.result.hex + "\"] }";
                        dynamic dTXID = JObject.Parse(Utility.rpcExec(command));


                    }
            }


        }
    }
}