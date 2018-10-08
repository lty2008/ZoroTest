﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace example
{
    class TransferOneNEO : IExample
    {
        public string Name => "转账1Neo";

        public string ID => "tran neo";

        async public Task Start()
        {
            string wif = "L3yQVZKu7u1etBWbeqfNeX1d19mRJdjZTZxiFt72AhLvxBDJwmne";
            string targetAddress = "ARuWRG39dd364tDpwqVZuyxur8VG2wHa2Q";
            decimal sendCount = new decimal(1);

            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);


            //获取自己的utxo
            Dictionary<string, List<UTXO>> dic_UTXO = await HttpHelper.GetUTXOByAddress(address);
            //拼装交易体
            Transaction tran = makeTran(dic_UTXO, address, targetAddress,new ThinNeo.Hash256("0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b"), sendCount);
            tran.version = 0;
            tran.attributes = new ThinNeo.Attribute[0];
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            byte[] msg = tran.GetMessage();
            string msgstr = ThinNeo.Helper.Bytes2HexString(msg);
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            byte[] postdata;
            var url = HttpHelper.MakeRpcUrlPost("http://182.254.221.14:10332", "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            var result = await HttpHelper.HttpPost(url, postdata);
            MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
            Console.WriteLine(resJO.ToString());
        }


        //拼交易体
        Transaction makeTran(Dictionary<string, List<UTXO>> dir_utxos, string fromAddress, string targetAddress, ThinNeo.Hash256 assetid, decimal sendcount)
        {
            if (!dir_utxos.ContainsKey(assetid.ToString()))
                throw new Exception("no enough money.");

            List<UTXO> utxos = dir_utxos[assetid.ToString()];
            
            Transaction tran = new Transaction();


            utxos.Sort((a, b) =>
            {
                if (a.value > b.value)
                    return 1;
                else if (a.value < b.value)
                    return -1;
                else
                    return 0;
            });

            decimal count = decimal.Zero;
            List<TransactionInput> list_inputs = new List<TransactionInput>();

            Console.WriteLine(utxos.Count);

            for (var i = 0; i < utxos.Count; i++)
            {
                TransactionInput input = new TransactionInput();
                input.hash = utxos[i].txid;
                input.index = (ushort)utxos[i].n;
                list_inputs.Add(input);
                count += utxos[i].value;
                if (count >= (sendcount))
                {
                    break;
                }
            }

            tran.inputs = list_inputs.ToArray();

            Console.WriteLine(count);

            if (count >= sendcount)//输入大于等于输出
            {
                List<TransactionOutput> list_outputs = new List<TransactionOutput>();
                //输出
                if (sendcount > decimal.Zero)
                {
                    TransactionOutput output = new TransactionOutput();
                    output.assetId = assetid;
                    output.value = sendcount;
                    output.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetAddress);
                    list_outputs.Add(output);
                }

                //找零
                var change = count - sendcount;
                if (change > decimal.Zero)
                {
                    TransactionOutput outputchange = new TransactionOutput();
                    outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(fromAddress);
                    outputchange.value = change;
                    outputchange.assetId = assetid;
                    list_outputs.Add(outputchange);
                }
                tran.outputs = list_outputs.ToArray();
            }
            else
            {
                throw new Exception("no enough money.");
            }
            return tran;
        }
    }
}
