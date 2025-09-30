using System;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices; //for import c++ dll


using GLU64 = System.UInt64;
using GLU32 = System.UInt32;
using GLU16 = System.UInt16;
using GLU8 = System.Byte;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap;
using System.Threading;
using System.Linq;

namespace EGMENGINE.SASCTLModule.SASClient.GanlotSASClient.GXGSASAPIWrap
{
    internal class GXGSASROMSigCalculator
    {
        private bool m_isThreadStart;
        private int m_calculateTime;
        Thread thread1;

        private static ushort CRC(byte[] s, int len, ushort crcval)
        {
            ushort c, q;

            for (; len > 0; len--)
            {
                c = s[s.Length - len];

                // Aplicando máscara de 4 bits
                q = (ushort)((crcval ^ c) & 0xF); // 0xF = 15 en decimal
                crcval = (ushort)((crcval >> 4) ^ (q * 0x1021)); // 0x1021 = 4129 en decimal (polinomio común para CRC-16-CCITT)

                q = (ushort)((crcval ^ (c >> 4)) & 0xF);
                crcval = (ushort)((crcval >> 4) ^ (q * 0x1021));
            }

            return crcval;
        }
        private static ushort CalculateCRCFiles(string[] files, ushort seed)
        {
            byte[] linea = new byte[] { };

            foreach (string s in files)
            {
                string rutaArchivo = s;  // Reemplaza con la ruta real de tu archivo
                try
                {
                    linea = linea.Concat(File.ReadAllBytes(rutaArchivo)).ToArray();

                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"Error: No se encontró el archivo en la ruta '{rutaArchivo}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            ushort result = seed;
            if (linea.Length > 0)
            {
                result = CRC(linea, linea.Length, seed);
                result = CRC(linea, linea.Length, seed);
            }

            seed = result;


            return seed;


        }


        private int CalculateROMSig(GLU8 port, GLU16 seed)
        {
            GLU32 dwStatus;
            int count100MS = 0;

            while (count100MS < (m_calculateTime * 10))
            {
                /*
                 * NOTE: We have to check if host update the seed while we are under calculation
                 *
                 * In this example, we do not really check the critical data. We just demo how to
                 * handle the ROM signature process.
                 */
                if (GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[port] > 0)
                    return 1;
                count100MS++;
                Thread.Sleep(100);
            }

            ushort result = CalculateCRCFiles(new string[] { "TestHarnessNet2_Data\\Managed\\EGMEngine.dll"}, seed);

            /*
             * In this example, we do not really check the critical data.
             * We use the seed to be the calculation result.
             */
            dwStatus = GXGSASAPIBridge.GXG_SAS_SetROMSig(port, result, seed);
            /*
            * If we get this error, it means the time between we set ROM signature result and host
            * update new seed is almost the same. We have to calculate again.
            */
            if (dwStatus == 0x4006)
            {
                return 1;
            }

            return 0;

        }

        /* Signal */

        void SendTraceMsg(string str, int type)
        {

        }

        /* Protected */

        void run()
        {
            try
            {


                int i = 0;

                while (m_isThreadStart == true)
                {
                    for (i = 0; i < 3; i++)
                    {
                        if (GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] > 0)
                        {
                            /*
                             * NOTE: If you receive a new seed before you finish the calculation, you
                             * have to use the new seed to calculate again.
                             */
                            GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] = GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] - 1;
                            while (GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] != 0)
                            {
                                GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] = GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] - 1;
                            };

                            while (this.CalculateROMSig((byte)i, GXGSASGlobalVariables.g_wROMSigSeed[i]) == 1 /* ROM_CALCULATE_SEED_CHANGED */)
                            {
                                GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] = GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] - 1;
                                while (GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] != 0)
                                {
                                    GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] = GXGSASGlobalVariables.g_atIsGetROMSigReqCnt[i] - 1;
                                };
                            }
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (ThreadInterruptedException ex)
            {
                return;
            }
        }


        public GXGSASROMSigCalculator()
        {
            m_isThreadStart = true;
            m_calculateTime = 5;


        }

        public void SetCalculateTimeS(int sec)
        {
            m_calculateTime = sec;
        }

        public void StartThread()
        {
            thread1 = new Thread(run);
            thread1.Start();
        }
        public void StopThread()
        {
            m_isThreadStart = false;
        }



    }

}

