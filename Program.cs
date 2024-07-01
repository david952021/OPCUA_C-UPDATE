using Opc.Ua;
using OpcUaHelper;
using System;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Timers;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpcUaHelperGetNoteIdTest
{
    internal class Program
    {
        static OpcUaClient opcUaClient ;
        private static System.Timers.Timer heartbeatTimer;
        private static string uri = "opc.tcp://10.82.51.3:4840";
        public static int lineCount = 0;
        /*public  static string currenttime = DateTime.Now.ToString().Replace("/", "").Replace(" ", "0").Replace(":", "");*/
        /*public static string outputPath = @"d:\5\" + currenttime + ".txt";*/

        static void Main(string[] args)
        {
           
            opcUaClient = new OpcUaClient();
            opcUaClient.UserIdentity = new UserIdentity(new AnonymousIdentityToken());
            opcUaClient.ConnectServer(uri);

            if (opcUaClient.Connected)
            {
                heartbeatTimer = new System.Timers.Timer(3000); // 设置心跳间隔为5秒
                heartbeatTimer.Elapsed += HeartbeatTick;
                heartbeatTimer.Start(); // 开始定时器
                outputBuilder.Length = 0;
                Recursive();
            }
            else
            {
                Console.WriteLine("网络断开，请检查！！！，联网后请重启程序！！！");
            }
            Console.ReadLine();
        }

        private static void HeartbeatTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                // 更新心跳操作
                outputBuilder.Length = 0;
                // 如果连接未断开，执行数据读取
                if (opcUaClient.Connected)
                {
                    Recursive();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in heartbeat: {ex.Message}");
                // 如果连接中断，停止定时器并重新尝试连接
                if (!opcUaClient.Connected)
                {
                    heartbeatTimer.Stop();
                    opcUaClient.ConnectServer(uri);
                }
            }
        }

        /// <param name="nodeID"></param>
        private static StringBuilder outputBuilder = new StringBuilder();
        private static string savePath = "";
        /*public static void Recursive(Action<NodeId> addNodeId)*/
        public static void Recursive()
        {
            List<NodeId> globalNodeIds = new List<NodeId>();
            try
            {
                // 添加所有的读取的节点
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.actual.cycleStart"));//切割机运行时为True
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.actual.cycleStop"));//切割机空闲时为true
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.messages[1].id"));//报警信息 0：正常1：报警
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.actual.opMode"));//工作模式  1：自动 2：编辑 3：MDI 4：DNC 5：手轮 6:手动
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.actual.position.x"));//机械坐标
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.partProgram.filename"));//当前程序号
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.actual.elapsedTime"));//加工时间   （秒）
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.partProgram.planSizeX"));//板材规格
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.partProgram.planSizeY"));//板材规格
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.partProgram.material"));//板材规格
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.actual.cycleStart"));//切割状态    0：未切割 1：切割
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.partProgram.thickness"));//切割厚度
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.plasma[1].arcVoltage"));//工作电压 V
                globalNodeIds.Add(new NodeId("ns=4;s=.PDC_Read.plasma[1].analogCurrent"));//工作电流  A

                // dataValues按顺序定义的值，每个值里面需要重新判断类型
                List<DataValue> dataValues = opcUaClient.ReadNodes(globalNodeIds.ToArray());
                string currenttime = DateTime.Now.ToString();
                string workNanme = "切割机运行;切割机空闲;报警信息;工作模式;机械坐标;当前程序号;加工时间(s);板材规格x;板材规格y;板材规格;切割状态;切割厚度(mm);工作电压;工作电流;";
                if (dataValues != null)
                {
                    
                    if (lineCount ==0) 
                    {
                        string outputPath = GenerateFilePath(0); // 生成文件路径
                        savePath = outputPath;
                        outputBuilder.AppendLine(workNanme);
                    }
                    
                    if (lineCount >= 1000)
                    {
                        Console.Clear();
                        Console.WriteLine("写入到下一文件" + lineCount);
                        string outputPath = GenerateFilePath(0); // 生成文件路径
                        savePath = outputPath;
                        outputBuilder.AppendLine(workNanme);
                        lineCount = 0;
                    }

                    var values = dataValues.Select(item => item.Value).ToList(); // 使用ToList预先获取所有值
                    string delimiter = ", ";
                    string output = string.Join(delimiter, dataValues.Select(item => item.Value)) + "\r\n";
                   
                    outputBuilder.AppendLine($"{currenttime}:{output}");
                    /*Console.WriteLine("数据追加写入" + lineCount + savePath);*/
                    Console.WriteLine(outputBuilder.ToString());
                    using (StreamWriter writer = new StreamWriter(savePath, true)) // 添加true参数表示追加模式1
                    {
                        writer.WriteLine(outputBuilder.ToString());
                    }

                    /*Console.WriteLine($"Output saved to {savePath}");*/
                    lineCount++;
                }
                else
                {
                    Console.WriteLine($"数据为空");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading node: {ex.Message}");
            }
        }

        private static string GenerateFilePath(int lineCount)
        {
            string directory = @"d:\5\"; // 缓存文件路径
            string fileName = "output_";
            string currentTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            return Path.Combine(directory, $"{fileName}{currentTime}.txt");
        }

    }
}
