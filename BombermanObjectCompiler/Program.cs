﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Linq;

namespace BombermanObjectCompiler
{
    /*
     * TODO
     * ----
     * - Generate Basic Header without DL offset (just make it 0)
     * - Add in textures and fill in textureoffsets
     * - Add in vertices and fill in verticeoffsets
     * - Parse every displaylist
     * - Add them to the byte array
     * - Write byte array and be happy :)
     */

    internal class Program
    {
        public struct VTX
        {
            public VTX_Item[] Vertex;
            public UInt64 VertexOffset;
            public string Identifier;
        }
        public struct VTX_Item
        {
            public Vector3 Pos;
            public ushort flag;
            public Vector2 Coords;
            public Vector4 Colours;
        }

        public struct Tex
        {
            public UInt64[] Texture;
            public UInt64 TexOffset;
            public string Identifier;
        }

        public struct DL
        {
            public int Offset;
            public byte[] Data;
        }

        public static int MainDLOffset;
        public static int CurDLOffset;
        public static Dictionary<string, byte> ImageFormats = new Dictionary<string, byte>();
        public static Dictionary<string, byte> SizeFormats = new Dictionary<string, byte>();
        public static Dictionary<string, byte> TextureMicrocodes = new Dictionary<string, byte>();
        public static Dictionary<string, int> OthermodeValues = new Dictionary<string, int>();
        public static Dictionary<string, byte> CombinerValues = new Dictionary<string, byte>();
        public static Dictionary<string, byte> CombinerValuesAlpha = new Dictionary<string, byte>();

        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Please give the path of the folder containing the model.inc.c and header.h file in the arguments.");
                Environment.Exit(1);
            }

            Console.WriteLine("Compiling...");
            try
            {
                List<string> Header = new List<string>(File.ReadAllLines((args[0] + "\\header.h")));
                List<string> MainFile = new List<string>(File.ReadAllLines((args[0] + "\\model.inc.c")));

                List<byte> EndResult = new List<byte>();
                /*
                 * u64 = texture 
                 * VTX = vertices
                 * GFX = DL
                 * SPLIT DLS INTO TINY ONES!
                 * main DL = last one
                */
                Dictionary<string, Tex> TexturePairs = new Dictionary<string, Tex>();
                Dictionary<string, VTX> Vertices = new Dictionary<string, VTX>();
                List<byte> OutData = new List<byte>();
                int Index = 0;

                foreach (string line in Header)
                {
                    switch(line.Split(' ')[1])
                    {
                        case "u64":
                            {
                                ParseTexture(MainFile, line, ref TexturePairs);
                                break;
                            }
                        case "Vtx":
                            {
                                ParseVTX(MainFile, line, ref Vertices);
                                break;
                            }
                        case "Gfx":
                            {
                                MainDLOffset = FindResourceLine(MainFile, TrimExcess(line) + "[] =");
                                MainDLOffset = MainDLOffset + 1;
                                /*
                                Console.WriteLine($"Pre splitting GFX line {TrimExcess(line)} if needed...");
                                //copy to temp buffer
                                List<string> tmpBuf = new List<string>();
                                //find end of DL so we can copy
                                int DLEnd = 0;
                                for(int i = MainDLOffset; i < MainFile.Count; i++)
                                {
                                    if(GetCommand(MainFile[i]) == "gsSPEndDisplayList")
                                    {
                                        DLEnd = i+1;
                                        break;
                                    }
                                }

                                tmpBuf.AddRange(MainFile.GetRange(MainDLOffset, DLEnd - MainDLOffset));

                                List<string> OverWriteBuf = new List<string>();
                                List<Vector2> VTXLocs = new List<Vector2>();
                                int VTXAmm = 0;
                                int RelativeDLEnd = 0;
                                for(int i = 0; i < tmpBuf.Count; i++)
                                {                                    
                                    if(GetCommand(tmpBuf[i]) == "gsSPVertex")
                                    {
                                        Vector2 v = new Vector2();
                                        v.X = i;
                                        VTXLocs.Add(v);
                                        VTXAmm++;
                                    }
                                    if(GetCommand(tmpBuf[i]) == "gsSPEndDisplayList")
                                    {
                                        RelativeDLEnd = i;
                                    }
                                }
                                if(VTXAmm <= 1)
                                {
                                    Console.WriteLine("No need to split...");
                                    break;
                                } //check if splitting is needed                                

                                //iterate through all items
                                for(int i = 0; i < VTXLocs.Count; i++)
                                {
                                    OverWriteBuf.Add($"{TrimExcess(line).Replace("mesh","splitmesh")}_split_{i}[] = " + '{');
                                    if(i != VTXLocs.Count - 1)
                                    {
                                        OverWriteBuf.AddRange(tmpBuf.GetRange((int)VTXLocs[i].X, (int)VTXLocs[i + 1].X - 1 - (int)VTXLocs[i].X));
                                    }         
                                    else
                                    {
                                        OverWriteBuf.AddRange(tmpBuf.GetRange((int)VTXLocs[i].X, RelativeDLEnd - 1 - (int)VTXLocs[i].X));
                                    }
                                    OverWriteBuf.Add("gsSPEndDisplayList(),");
                                    OverWriteBuf.Add("};");
                                    OverWriteBuf.Add("");
                                }

                                Console.WriteLine($"Split GFX line into {VTXAmm} separate DLs");
                                OverWriteBuf.Add(TrimExcess(line) + "[] = " + '{');
                                for(int i = 0; i < VTXAmm; i++)
                                {
                                    OverWriteBuf.Add($"gsSPDisplayList({TrimExcess(line).Replace("mesh", "splitmesh").Replace("Gfx ","")}_split_{i}),");
                                }
                                OverWriteBuf.Add("gsSPEndDisplayList(),");
                                OverWriteBuf.Add("};");

                                //now take out the original function and replace it with my new one
                                MainFile.RemoveRange(MainDLOffset - 1, DLEnd - MainDLOffset + 2);
                                MainFile.InsertRange(MainDLOffset, OverWriteBuf);
                                */
                                break;
                            }
                    }
                    Index++;
                }
                
                for(int i = 0; i < MainFile.Count; i++)
                {
                    if(MainFile[i].Contains("gsDPSetTextureLUT("))
                    {
                        string f = MainFile[i].Replace("gsDPSetTextureLUT(", "gsSPSetOtherMode(G_SETOTHERMODE_H, G_MDSFT_TEXTLUT, 2, ");
                        MainFile[i] = f;
                    }
                    if(MainFile[i].Contains("gsDPSetCycleType("))
                    {
                        string f = MainFile[i].Replace("gsDPSetCycleType(", "gsSPSetOtherMode(G_SETOTHERMODE_H, G_MDSFT_CYCLETYPE, 2, ");
                        MainFile[i] = f;
                    }
                    if (MainFile[i].Contains("gsDPSetTextureLOD("))
                    {
                        string f = MainFile[i].Replace("gsDPSetTextureLOD(", "gsSPSetOtherMode(G_SETOTHERMODE_H, G_MDSFT_TEXTLOD, 1, ");
                        MainFile[i] = f;
                    }
                    if (MainFile[i].Contains("gsDPSetTexturePersp("))
                    {
                        string f = MainFile[i].Replace("gsDPSetTexturePersp(", "gsSPSetOtherMode(G_SETOTHERMODE_H, G_MDSFT_TEXTPERSP, 1, ");
                        MainFile[i] = f;
                    }
                }

                MainDLOffset = FindResourceLine(MainFile, "Gfx " +TrimExcess(Header[Header.Count() - 1].Split(' ')[2]) + "[] =");
                MainDLOffset++;

                Console.WriteLine("Textures & Vertices parsed...");

                OutData.AddRange(new byte[] { 0x36, 0x34, 0x00, 0x38 });
                OutData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x01 }); //1 length DL for now
                OutData.AddRange(new byte[] { 0x02, 0x02, 0x02, 0x02 });

                OutData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });
                OutData.AddRange(new byte[] { 0x3F, 0x80, 0x00, 0x00 });
                OutData.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }); // replace this at the end

                for(int i = 0; i < TexturePairs.Count; i++)
                {
                    Tex tex = TexturePairs.ElementAt(i).Value;
                    tex.TexOffset = (ulong)OutData.Count;
                    TexturePairs[tex.Identifier] = tex;

                    foreach(UInt64 Data in tex.Texture)
                    {
                        byte[] buf = BitConverter.GetBytes(Data);
                        if(BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);
                    }
                }

                Console.WriteLine("Texture Data parsed");

                for (int i = 0; i < Vertices.Count; i++)
                {
                    VTX vtx = Vertices.ElementAt(i).Value;
                    vtx.VertexOffset = (ulong)OutData.Count;
                    Vertices[vtx.Identifier] = vtx;

                    foreach(VTX_Item item in vtx.Vertex)
                    {
                        short X = (short)item.Pos.X;
                        short Y = (short)item.Pos.Y;
                        short Z = (short)item.Pos.Z;
                        ushort flag = (ushort)item.flag;
                        short TX = (short)item.Coords.X;
                        short TY = (short)item.Coords.Y;
                        byte R = (byte)item.Colours.X;
                        byte G = (byte)item.Colours.Y;
                        byte B = (byte)item.Colours.Z;
                        byte A = (byte)item.Colours.W;

                        byte[] buf = BitConverter.GetBytes(X);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);

                        buf = BitConverter.GetBytes(Y);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);

                        buf = BitConverter.GetBytes(Z);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);

                        buf = BitConverter.GetBytes(flag);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);

                        buf = BitConverter.GetBytes(TX);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);

                        buf = BitConverter.GetBytes(TY);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        OutData.AddRange(buf);

                        OutData.Add(R);
                        OutData.Add(G);
                        OutData.Add(B);
                        OutData.Add(A);
                    }
                }

                Console.WriteLine("Vertex Data parsed");

                #region DEFINES
                ImageFormats.Add("G_IM_FMT_RGBA", 0);
                ImageFormats.Add("G_IM_FMT_YUV", 1);
                ImageFormats.Add("G_IM_FMT_CI", 2);
                ImageFormats.Add("G_IM_FMT_IA", 3);
                ImageFormats.Add("G_IM_FMT_I", 4);
                ImageFormats.Add("0", 0);

                SizeFormats.Add("G_IM_SIZ_4b", 0);
                SizeFormats.Add("G_IM_SIZ_8b", 1);
                SizeFormats.Add("G_IM_SIZ_16b", 2);
                SizeFormats.Add("G_IM_SIZ_32b", 3);
                SizeFormats.Add("G_IM_SIZ_DD", 5);

                SizeFormats.Add("G_IM_SIZ_4b_BYTES", 0);
                SizeFormats.Add("G_IM_SIZ_4b_TILE_BYTES", 0);
                SizeFormats.Add("G_IM_SIZ_4b_LINE_BYTES", 0);

                SizeFormats.Add("G_IM_SIZ_8b_BYTES", 1);
                SizeFormats.Add("G_IM_SIZ_8b_TILE_BYTES", 1);
                SizeFormats.Add("G_IM_SIZ_8b_LINE_BYTES", 1);

                SizeFormats.Add("G_IM_SIZ_16b_BYTES", 2);
                SizeFormats.Add("G_IM_SIZ_16b_TILE_BYTES", 2);
                SizeFormats.Add("G_IM_SIZ_16b_LINE_BYTES", 2);

                SizeFormats.Add("G_IM_SIZ_32b_BYTES", 4);
                SizeFormats.Add("G_IM_SIZ_32b_TILE_BYTES", 2);
                SizeFormats.Add("G_IM_SIZ_32b_LINE_BYTES", 2);

                SizeFormats.Add("G_IM_SIZ_4b_LOAD_BLOCK", 2);
                SizeFormats.Add("G_IM_SIZ_8b_LOAD_BLOCK", 2);
                SizeFormats.Add("G_IM_SIZ_16b_LOAD_BLOCK", 2);
                SizeFormats.Add("G_IM_SIZ_32b_LOAD_BLOCK", 3);

                SizeFormats.Add("G_IM_SIZ_4b_SHIFT", 2);
                SizeFormats.Add("G_IM_SIZ_8b_SHIFT", 1);
                SizeFormats.Add("G_IM_SIZ_16b_SHIFT", 0);
                SizeFormats.Add("G_IM_SIZ_32b_SHIFT", 0);

                SizeFormats.Add("G_IM_SIZ_4b_INCR", 3);
                SizeFormats.Add("G_IM_SIZ_8b_INCR", 1);
                SizeFormats.Add("G_IM_SIZ_16b_INCR", 0);
                SizeFormats.Add("G_IM_SIZ_32b_INCR", 0);
                SizeFormats.Add("0", 0);

                TextureMicrocodes.Add("G_TX_LOADTILE", 7);
                TextureMicrocodes.Add("G_TX_RENDERTILE", 0);
                TextureMicrocodes.Add("G_TX_NOMIRROR", 0);
                TextureMicrocodes.Add("G_TX_WRAP", 0);
                TextureMicrocodes.Add("G_TX_MIRROR", 1);
                TextureMicrocodes.Add("G_TX_CLAMP", 2);
                TextureMicrocodes.Add("G_TX_NOMASK", 0);
                TextureMicrocodes.Add("G_TX_NOLOD", 0);

                OthermodeValues.Add("G_MDSFT_ALPHACOMPARE", 0);
                OthermodeValues.Add("G_MDSFT_ZSRCSEL", 2);
                OthermodeValues.Add("G_MDSFT_RENDERMODE", 3);
                OthermodeValues.Add("G_MDSFT_BLENDER", 16);
                OthermodeValues.Add("G_MDSFT_BLENDMASK", 0);
                OthermodeValues.Add("G_MDSFT_ALPHADITHER", 4);
                OthermodeValues.Add("G_MDSFT_RGBDITHER", 6);
                OthermodeValues.Add("G_MDSFT_COMBKEY", 8);
                OthermodeValues.Add("G_MDSFT_TEXTCONV", 9);
                OthermodeValues.Add("G_MDSFT_TEXTFILT", 12);
                OthermodeValues.Add("G_MDSFT_TEXTLUT", 14);
                OthermodeValues.Add("G_MDSFT_TEXTLOD", 16);
                OthermodeValues.Add("G_MDSFT_TEXTDETAIL", 17);
                OthermodeValues.Add("G_MDSFT_TEXTPERSP", 19);
                OthermodeValues.Add("G_MDSFT_CYCLETYPE", 20);
                OthermodeValues.Add("G_MDSFT_COLORDITHER", 22);
                OthermodeValues.Add("G_MDSFT_PIPELINE", 23);

                OthermodeValues.Add("G_PM_1PRIMITIVE", 1 << OthermodeValues["G_MDSFT_PIPELINE"]);
                OthermodeValues.Add("G_PM_NPRIMITIVE", 0 << OthermodeValues["G_MDSFT_PIPELINE"]);

                OthermodeValues.Add("G_CYC_1CYCLE", 0 << OthermodeValues["G_MDSFT_CYCLETYPE"]);
                OthermodeValues.Add("G_CYC_2CYCLE", 1 << OthermodeValues["G_MDSFT_CYCLETYPE"]);
                OthermodeValues.Add("G_CYC_COPY", 2 << OthermodeValues["G_MDSFT_CYCLETYPE"]);
                OthermodeValues.Add("G_CYC_FILL", 3 << OthermodeValues["G_MDSFT_CYCLETYPE"]);

                OthermodeValues.Add("G_TP_NONE", 0 << OthermodeValues["G_MDSFT_TEXTPERSP"]);
                OthermodeValues.Add("G_TP_PERSP", 1 << OthermodeValues["G_MDSFT_TEXTPERSP"]);

                OthermodeValues.Add("G_TD_CLAMP", 0 << OthermodeValues["G_MDSFT_TEXTDETAIL"]);
                OthermodeValues.Add("G_TD_SHARPEN", 1 << OthermodeValues["G_MDSFT_TEXTDETAIL"]);
                OthermodeValues.Add("G_TD_DETAIL", 2 << OthermodeValues["G_MDSFT_TEXTDETAIL"]);

                OthermodeValues.Add("G_TL_TILE", 0 << OthermodeValues["G_MDSFT_TEXTLOD"]);
                OthermodeValues.Add("G_TL_LOD", 1 << OthermodeValues["G_MDSFT_TEXTLOD"]);

                OthermodeValues.Add("G_TT_NONE", 0 << OthermodeValues["G_MDSFT_TEXTLUT"]);
                OthermodeValues.Add("G_TT_RGBA16", 2 << OthermodeValues["G_MDSFT_TEXTLUT"]);
                OthermodeValues.Add("G_TT_IA16", 3 << OthermodeValues["G_MDSFT_TEXTLUT"]);

                OthermodeValues.Add("G_TF_POINT", 0 << OthermodeValues["G_MDSFT_TEXTFILT"]);
                OthermodeValues.Add("G_TF_AVERAGE", 3 << OthermodeValues["G_MDSFT_TEXTFILT"]);
                OthermodeValues.Add("G_TF_BILERP", 2 << OthermodeValues["G_MDSFT_TEXTFILT"]);

                OthermodeValues.Add("G_TC_CONV", 0 << OthermodeValues["G_MDSFT_TEXTCONV"]);
                OthermodeValues.Add("G_TC_FILTCONV", 5 << OthermodeValues["G_MDSFT_TEXTCONV"]);
                OthermodeValues.Add("G_TC_FILT", 6 << OthermodeValues["G_MDSFT_TEXTCONV"]);

                OthermodeValues.Add("G_CK_NONE", 0 << OthermodeValues["G_MDSFT_COMBKEY"]);
                OthermodeValues.Add("G_CK_KEY", 1 << OthermodeValues["G_MDSFT_COMBKEY"]);

                OthermodeValues.Add("G_CD_MAGICSQ", 0 << OthermodeValues["G_MDSFT_RGBDITHER"]);
                OthermodeValues.Add("G_CD_BAYER", 1 << OthermodeValues["G_MDSFT_RGBDITHER"]);
                OthermodeValues.Add("G_CD_NOISE", 2 << OthermodeValues["G_MDSFT_RGBDITHER"]);

                OthermodeValues.Add("G_CD_DISABLE", 3 << OthermodeValues["G_MDSFT_RGBDITHER"]);
                OthermodeValues.Add("G_CD_ENABLE", OthermodeValues["G_CD_NOISE"]);

                OthermodeValues.Add("G_AD_PATTERN", 0 << OthermodeValues["G_MDSFT_ALPHADITHER"]);
                OthermodeValues.Add("G_AD_NOTPATTERN", 1 << OthermodeValues["G_MDSFT_ALPHADITHER"]);
                OthermodeValues.Add("G_AD_NOISE", 2 << OthermodeValues["G_MDSFT_ALPHADITHER"]);
                OthermodeValues.Add("G_AD_DISABLE", 3 << OthermodeValues["G_MDSFT_ALPHADITHER"]);

                OthermodeValues.Add("G_AC_NONE", 0 << OthermodeValues["G_MDSFT_ALPHACOMPARE"]);
                OthermodeValues.Add("G_AC_THRESHOLD", 1 << OthermodeValues["G_MDSFT_ALPHACOMPARE"]);
                OthermodeValues.Add("G_AC_DITHER", 3 << OthermodeValues["G_MDSFT_ALPHACOMPARE"]);

                OthermodeValues.Add("G_ZS_PIXEL", 0 << OthermodeValues["G_MDSFT_ZSRCSEL"]);
                OthermodeValues.Add("G_ZS_PRIM", 1 << OthermodeValues["G_MDSFT_ZSRCSEL"]);

                OthermodeValues.Add("AA_EN", 0x8);
                OthermodeValues.Add("Z_CMP", 0x10);
                OthermodeValues.Add("Z_UPD", 0x20);
                OthermodeValues.Add("IM_RD", 0x40);
                OthermodeValues.Add("CLR_ON_CVG", 0x80);
                OthermodeValues.Add("CVG_DST_CLAMP", 0x0);
                OthermodeValues.Add("CVG_DST_WRAP", 0x100);
                OthermodeValues.Add("CVG_DST_FULL", 0x200);
                OthermodeValues.Add("CVG_DST_SAVE", 0x300);
                OthermodeValues.Add("ZMODE_OPA", 0x00);
                OthermodeValues.Add("ZMODE_INTER", 0x400);
                OthermodeValues.Add("ZMODE_XLU", 0x800);
                OthermodeValues.Add("ZMODE_DEC", 0xc00);
                OthermodeValues.Add("CVG_X_ALPHA", 0x1000);
                OthermodeValues.Add("ALPHA_CVG_SEL", 0x2000);
                OthermodeValues.Add("FORCE_BL", 0x4000);
                OthermodeValues.Add("TEX_EDGE", 0x0000);


                //FC values part 1
                CombinerValues.Add("COMBINED", 0);
                CombinerValues.Add("TEXEL0", 1);
                CombinerValues.Add("TEXEL1", 2);
                CombinerValues.Add("PRIMITIVE", 3);
                CombinerValues.Add("SHADE", 4);
                CombinerValues.Add("ENVIRONMENT", 5);
                CombinerValues.Add("CENTER", 6);
                CombinerValues.Add("SCALE", 6);
                CombinerValues.Add("COMBINED_ALPHA", 7);
                CombinerValues.Add("TEXEL0_ALPHA", 8);
                CombinerValues.Add("TEXEL1_ALPHA", 9);
                CombinerValues.Add("PRIMITIVE_ALPHA", 10);
                CombinerValues.Add("SHADE_ALPHA", 11);
                CombinerValues.Add("ENV_ALPHA", 12);
                CombinerValues.Add("LOD_FRACTION", 13);
                CombinerValues.Add("PRIM_LOD_FRAC", 14);
                CombinerValues.Add("NOISE", 7);
                CombinerValues.Add("K4", 7);
                CombinerValues.Add("K5", 15);
                CombinerValues.Add("1", 6);
                CombinerValues.Add("0", 31);

                //FC values part 2
                CombinerValuesAlpha.Add("COMBINED", 0);
                CombinerValuesAlpha.Add("TEXEL0", 1);
                CombinerValuesAlpha.Add("TEXEL1", 2);
                CombinerValuesAlpha.Add("PRIMITIVE", 3);
                CombinerValuesAlpha.Add("SHADE", 4);
                CombinerValuesAlpha.Add("ENVIRONMENT", 5);
                CombinerValuesAlpha.Add("LOD_FRACTION", 0);
                CombinerValuesAlpha.Add("PRIM_LOD_FRAC", 6);
                CombinerValuesAlpha.Add("1", 6);
                CombinerValuesAlpha.Add("0", 7);

                #endregion

                File.WriteAllLines(args[0] + "\\tmp.c", MainFile.ToArray());
                ParseDL(MainFile, MainDLOffset, TexturePairs, Vertices, ref OutData);

                byte[] buffer = BitConverter.GetBytes(CurDLOffset);
                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }
                OutData[0x14] = buffer[0];
                OutData[0x15] = buffer[1];
                OutData[0x16] = buffer[2];
                OutData[0x17] = buffer[3];

                File.WriteAllBytes(args[0] + "\\outmodel.bin", OutData.ToArray());

                Console.WriteLine("File written to " + args[0] + "\\outmodel.bin");
                Console.WriteLine("Done!");
            }
            catch(Exception ex)
            { 
                Console.WriteLine(ex.ToString()); 
            }
        }
  
        public static void ParseDL(List<string> File, int Line, Dictionary<string, Tex> Textures, Dictionary<string, VTX> Vertices, ref List<byte> Model)
        {
            List<byte> OutData = new List<byte>();
            byte[] buf = new byte[1];
            while(buf[0] != 0xB8)
            {
                buf = ParseLine(File, Line, Textures, Vertices, ref Model);
                Line++;
                if(buf[0] != 0x10)
                {
                    OutData.AddRange(buf);
                }                
            }
            CurDLOffset = Model.Count;

            Model.AddRange(OutData);
        }

        /// <summary>
        /// Parses current DL line
        /// </summary>
        /// <param name="File">Full file</param>
        /// <param name="Line">Current line</param>
        /// <param name="Textures">Copy of all textures, for offsets</param>
        /// <param name="Vertices">Copy of all vertices, for offsets</param>
        /// <param name="Model">DO NOT USE TO EDIT MODEL FROM WITHIN FUNCTION. ONLY PASS FOR gsSPDisplayList</param>
        /// <returns>Data to add to the outfile.</returns>
        public static byte[] ParseLine(List<string> File, int Line, Dictionary<string, Tex> Textures, Dictionary<string, VTX> Vertices, ref List<byte> Model)
        {
            List<byte> Outdata = new List<byte>();

            switch(GetCommand(File[Line]))
            {
                case "gsSPClearGeometryMode":
                    {
                        Outdata.AddRange(new byte[] { 0xB6, 0x00, 0x00, 0x00 });
                        byte[] buf = BitConverter.GetBytes(GetGeometryMode(File[Line]));
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                case "gsSPVertex":
                    {
                        string[] Params = GetParams(File[Line]);

                        UInt32 VTXLine = (UInt32)Vertices["Vtx " + Params[0].Split('+')[0].Trim()].VertexOffset;
                        VTXLine += (UInt32.Parse(Params[0].Split('+')[1].Trim()) * 16); //SS values, load in bank 02

                        VTXLine += 0x02000000; //set bank

                        byte N = byte.Parse(Params[1]);
                        byte II = byte.Parse(Params[2]);

                        UInt16 XXXXX = 0;
                        XXXXX = (UInt16)(N << 10);
                        int L = (N * 0x10) - 1;
                        XXXXX |= (UInt16)L;
                        
                        Outdata.Add(0x04);
                        Outdata.Add(II);
                        byte[] buf = BitConverter.GetBytes(XXXXX);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        buf = BitConverter.GetBytes(VTXLine);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        break;
                    }
                case "gsSPEndDisplayList":
                    {
                        Outdata.AddRange(new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        break;
                    }
                case "gsSPCullDisplayList":
                    {
                        Outdata.Add(0xBE);
                        Outdata.Add(0x00);
                        string[] Params = GetParams(File[Line]);

                        ushort VV = ushort.Parse(Params[0].Trim());
                        byte[] buf = BitConverter.GetBytes(VV * 2);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.Add(buf[2]);
                        Outdata.Add(buf[3]);
                        Outdata.AddRange(new byte[] { 0x00, 0x00 });

                        ushort WW = ushort.Parse(Params[1].Trim());
                        buf = BitConverter.GetBytes(WW * 2);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.Add(buf[2]);
                        Outdata.Add(buf[3]);

                        break;
                    }
                case "gsDPPipeSync":
                    {
                        Outdata.AddRange(new byte[] { 0xE7, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        break;
                    }
                case "gsSPSetGeometryMode":
                    {
                        Outdata.AddRange(new byte[] { 0xB7, 0x00, 0x00, 0x00 });
                        byte[] buf = BitConverter.GetBytes(GetGeometryMode(File[Line]));
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                case "gsDPSetCombineLERP":
                    {                      
                        string[] Params = GetParams(File[Line]);
                        UInt64 MyData = 0xFC;
                        UInt64 a0 = CombinerValues[Params[0].Trim()];
                        UInt64 b0 = CombinerValues[Params[1].Trim()];
                        UInt64 c0 = CombinerValues[Params[2].Trim()];
                        UInt64 d0 = CombinerValues[Params[3].Trim()];
                        UInt64 Aa0 = CombinerValuesAlpha[Params[4].Trim()];
                        UInt64 Ab0 = CombinerValuesAlpha[Params[5].Trim()];
                        UInt64 Ac0 = CombinerValuesAlpha[Params[6].Trim()];
                        UInt64 Ad0 = CombinerValuesAlpha[Params[7].Trim()];
                        UInt64 a1 = CombinerValues[Params[8].Trim()];
                        UInt64 b1 = CombinerValues[Params[9].Trim()];
                        UInt64 c1 = CombinerValues[Params[10].Trim()];
                        UInt64 d1 = CombinerValues[Params[11].Trim()];
                        UInt64 Aa1 = CombinerValuesAlpha[Params[12].Trim()];
                        UInt64 Ab1 = CombinerValuesAlpha[Params[13].Trim()];
                        UInt64 Ac1 = CombinerValuesAlpha[Params[14].Trim()];
                        UInt64 Ad1 = CombinerValuesAlpha[Params[15].Trim()];

                        MyData = _SHIFTL(MyData, 24, 8);
                        UInt64 Combinervals = _SHIFTL(GCCc0w0(a0, c0, Aa0, Ac0) |
                                                      GCCc1w0(a1, c1), 0, 24);
                        MyData |= Combinervals;
                        MyData <<= 32;

                        Combinervals = GCCc0w1(b0, d0, Ab0, Ad0) |
                                       GCCc1w1(b1, Aa1, Ac1, d1, Ab1, Ad1);
                        MyData |= Combinervals;

                        //Console.WriteLine(MyData.ToString("X"));

                        byte[] wow = BitConverter.GetBytes(MyData);
                        if(BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(wow);
                        }
                        Outdata.AddRange(wow);

                        break;
                    }
                case "gsSPTexture":
                    {
                        string[] Params = GetParams(File[Line]);
                        ushort TTTT = ushort.Parse(Params[1]);
                        ushort SSSS = ushort.Parse(Params[0]);
                        byte NN = byte.Parse(Params[4]);

                        byte LLL = byte.Parse(Params[2]);
                        byte DDD = byte.Parse(Params[3]);

                        UInt64 Apple = 0xBB00;
                        Apple <<= 5;
                        Apple |= LLL;
                        Apple <<= 3;
                        Apple |= DDD;

                        Apple <<= 8;
                        Apple |= NN;

                        Apple <<= 16;
                        Apple |= SSSS;
                        Apple <<= 16;
                        Apple |= TTTT;

                        byte[] buf = BitConverter.GetBytes(Apple);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        break;
                    }
                case "gsSPDisplayList":
                    {
                        string obj = GetParams(File[Line])[0];
                        obj = "Gfx " + obj + "[]";
                        int LineToGive = FindResourceLine(File, obj);
                        LineToGive++;

                        ParseDL(File, LineToGive, Textures, Vertices, ref Model);
                        Outdata.Add(0x06);
                        Outdata.Add(0x00);
                        Outdata.Add(0x00);
                        Outdata.Add(0x00);

                        int BufOffset = CurDLOffset + 0x02000000;
                        byte[] buf = BitConverter.GetBytes(BufOffset);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        break;
                    }
                case "gsDPSetTextureImage":
                    {
                        //oh deary me, I feel like the W param is just poof here
                        string[] Params = GetParams(File[Line]);
                        byte BFormat = ImageFormats[Params[0].Trim()];
                        byte SFormat = SizeFormats[Params[1].Trim()];

                        byte XX = (byte)((BFormat << 5) | (SFormat << 3));
                        byte W = byte.Parse(Params[2].Trim());

                        UInt64 SegAddr = 2 << 24;
                        UInt64 AddrToFind = (UInt64)Textures["u64" + Params[3]].TexOffset;
                        SegAddr += AddrToFind;

                        UInt64 MyData = 0xFD;

                        MyData <<= 3; //format
                        MyData |= BFormat;
                        MyData <<= 2; //bit size
                        MyData |= SFormat;
                        MyData <<= 3; //re-align
                        MyData <<= 4; //move over half a byte
                        MyData <<= 4 + 8; //move over the rest
                        //MyData |= W; //set W parameter - redundant for this command
                        MyData <<= 8 * 4; //scoot over 4 bytes
                        MyData |= SegAddr;

                        byte[] buf = BitConverter.GetBytes(MyData);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        break;
                    }
                case "gsDPTileSync":
                    {
                        Outdata.AddRange(new byte[] { 0xE8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        break;
                    }
                case "gsDPSetTile":
                    {
                        string[] Params = GetParams(File[Line]);

                        UInt64 OutBuf = 0xF5;
                        OutBuf = OutBuf << 3;
                        OutBuf |= ImageFormats[Params[0].Trim()];

                        OutBuf = OutBuf << 2;
                        OutBuf |= SizeFormats[Params[1].Trim()];

                        OutBuf = OutBuf << 1;
                        OutBuf = OutBuf << 9;

                        ulong BitValues = ulong.Parse(Params[2]);
                        BitValues = (ulong)(BitValues & 0b0000000111111111);

                        OutBuf |= (ulong)BitValues;

                        BitValues = ulong.Parse(Params[3]);
                        BitValues = (ulong)(BitValues & 0b0000000111111111);
                        OutBuf = OutBuf << 9;
                        OutBuf |= (ulong)BitValues;

                        OutBuf = OutBuf << 5;

                        OutBuf = OutBuf << 3;
                        OutBuf |= byte.Parse(Params[4]);

                        OutBuf = OutBuf << 4;
                        OutBuf |= byte.Parse(Params[5]);

                        OutBuf = OutBuf << 2;
                        string[] TextureModes = Params[6].Split('|');
                        byte Descriptor = 0;
                        foreach (string s in TextureModes)
                        {
                            Descriptor |= (byte)TextureMicrocodes[s.Trim()];
                        }
                        OutBuf |= Descriptor;

                        OutBuf = OutBuf << 4;
                        OutBuf |= byte.Parse(Params[7]);

                        OutBuf = OutBuf << 4;
                        OutBuf |= byte.Parse(Params[8]);

                        OutBuf = OutBuf << 2;
                        TextureModes = Params[9].Split('|');
                        Descriptor = 0;
                        foreach (string s in TextureModes)
                        {
                            Descriptor |= (byte)TextureMicrocodes[s.Trim()];
                        }
                        OutBuf |= Descriptor;

                        OutBuf = OutBuf << 4;
                        OutBuf |= byte.Parse(Params[10]);

                        OutBuf = OutBuf << 4;
                        OutBuf |= byte.Parse(Params[11]);

                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                case "gsDPLoadSync":
                    {
                        Outdata.AddRange(new byte[] { 0xE6, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        break;
                    }
                case "gsDPLoadTLUTCmd":
                    {
                        UInt64 OutBuf = 0xF000000000000000;
                        string[] Params = GetParams(File[Line]);

                        byte Descriptor = byte.Parse(Params[0]);
                        OutBuf |= ((UInt64)Descriptor << 24);

                        ushort ColourCount = (ushort)((ushort.Parse(Params[1]) & 0x3FF) << 2);
                        OutBuf |= ((UInt64)ColourCount << 12);

                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        break;
                    }
                case "gsDPLoadBlock":
                    {
                        string[] Params = GetParams(File[Line]);
                        /*
                        UInt64 OutBuf = 0xF300000000000000;
                        
                        ushort Dat = ushort.Parse(Params[0]);
                        OutBuf |= (UInt64)Dat << 24;
                        Dat = ushort.Parse(Params[1]);
                        OutBuf |= (UInt64)Dat << 44;
                        Dat = ushort.Parse(Params[2]);
                        OutBuf |= (UInt64)Dat << 32;
                        Dat = ushort.Parse(Params[3]);
                        OutBuf |= (UInt64)Dat << 12;
                        Dat = ushort.Parse(Params[4]);
                        OutBuf |= (UInt64)Dat;
                        */

                        UInt64 OutBuf = 0xF3;
                        OutBuf <<= 8 + 4; //shift for S
                        OutBuf |= ushort.Parse(Params[1]);
                        OutBuf <<= 8 + 4; //shift for T
                        OutBuf |= ushort.Parse(Params[2]);
                        OutBuf <<= 8; //shift for tile descriptor
                        OutBuf |= byte.Parse(Params[0]);
                        OutBuf <<= 8 + 4; //shift for X
                        OutBuf |= ushort.Parse(Params[3]);
                        OutBuf <<= 8 + 4; //shift for D
                        OutBuf |= ushort.Parse(Params[4]);

                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                case "gsDPSetTileSize":
                    {
                        string[] Params = GetParams(File[Line]);

                        UInt64 OutBuf = 0xF200000000000000;
                        ushort Dat = ushort.Parse(Params[0]);
                        OutBuf |= (UInt64)Dat << 44;
                        Dat = ushort.Parse(Params[1]);
                        OutBuf |= (UInt64)Dat << 32;
                        Dat = ushort.Parse(Params[2]);
                        OutBuf |= (UInt64)Dat << 24;
                        Dat = ushort.Parse(Params[3]);
                        OutBuf |= (UInt64)Dat << 12;
                        Dat = ushort.Parse(Params[3]);
                        OutBuf |= (UInt64)Dat;

                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                case "gsSP2Triangles":
                    {
                        string[] Params = GetParams(File[Line]);

                        UInt64 OutBuf = 0xB100000000000000;

                        for (int i = 0; i < Params.Length - 1; i++)
                        {
                            byte dat = byte.Parse(Params[i]);
                            OutBuf |= ((UInt64)dat << 8 * (Params.Length - 2 - i)) * 2;
                        }


                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                case "gsSP1Triangle":
                    {
                        UInt64 OutBuf = 0xBF00000000000000;
                        string[] Params = GetParams(File[Line]);

                        for (int i = 0; i < Params.Length - 1; i++)
                        {
                            byte dat = byte.Parse(Params[i]);
                            OutBuf |= ((UInt64)dat << 8 * (Params.Length - 2 - i)) * 2;
                        }

                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        break;
                    }
                case "gsSPSetOtherMode":
                    {
                        string[] Params = GetParams(File[Line]);
                        UInt64 OutBuf = 0;
                        switch (Params[0])
                        {
                            case "G_SETOTHERMODE_H":
                                {
                                    OutBuf = 0xBA00000000000000;
                                    break;
                                }
                            case "G_SETOTHERMODE_L":
                                {
                                    OutBuf = 0xB900000000000000;
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Unknown other mode: " + Params[0]);
                                    Outdata.Add(0x10);
                                    return Outdata.ToArray();
                                }
                        }

                        OutBuf |= (UInt64)OthermodeValues[Params[1].Trim()] << 40;
                        OutBuf |= (UInt64)(UInt64.Parse(Params[2].Trim()) << 32);

                        string[] BitsToSet = Params[3].Split('|');
                        UInt64 Setter = 0;
                        foreach (string s in BitsToSet)
                        {
                            Setter |= (UInt64)(OthermodeValues[s.Trim()]);
                        }
                        OutBuf |= Setter;

                        byte[] buf = BitConverter.GetBytes(OutBuf);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);


                        break;
                    }
                case "G_SPNOOP":
                    {
                        Outdata.AddRange(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }); //debug command
                        break;
                    }
                case "G_RDPFULLSYNC":
                    {
                        Outdata.AddRange(new byte[] { 0xE9, 0, 0, 0, 0, 0, 0, 0 });
                        break;
                    }
                case "gsSPSetLights1":
                {
                        Outdata.Add(0x10);
                        break;
                }
                case "gsSPTextureRectangle":
                    {
                        string[] Params = GetParams(File[Line]);
                        UInt64 Command = 0xE4;
                        UInt64 xl = ulong.Parse(Params[0]);
                        UInt64 yl = ulong.Parse(Params[1]);
                        UInt64 xh = ulong.Parse(Params[2]);
                        UInt64 yh = ulong.Parse(Params[3]);
                        UInt64 tile = ulong.Parse(Params[4]);
                        UInt64 s = ulong.Parse(Params[5]);
                        UInt64 t = ulong.Parse(Params[6]);
                        UInt64 dsdx = ulong.Parse(Params[7]);
                        UInt64 dtdy = ulong.Parse(Params[8]);

                        Command = _SHIFTL(Command, 24 + 24 + 8, 8) | _SHIFTL(xh, 12 + 12 + 24, 12) | _SHIFTL(yh, 0 + 24 + 12, 12) |
                                  _SHIFTL(tile, 24, 3) | _SHIFTL(xl, 12, 12) | _SHIFTL(yl, 0, 12);
                        byte[] buf = BitConverter.GetBytes(Command);
                        if(BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        buf = BitConverter.GetBytes(gsImmp1(0xB4, _SHIFTL(s,16,16) | _SHIFTL(t,0,16)));
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);

                        buf = BitConverter.GetBytes(gsImmp1(0xB3, _SHIFTL(dsdx, 16, 16) | _SHIFTL(dtdy, 0, 16)));
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(buf);
                        }
                        Outdata.AddRange(buf);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Unknown command: " + File[Line]);
                        Outdata.Add(0x10);
                        break;
                    }
            }

            return Outdata.ToArray();
        }

        public static string[] GetParams(string FullLine)
        {
            string Line = FullLine.Substring(FullLine.IndexOf('(') + 1);
            Line = Line.Substring(0, Line.IndexOf(')'));

            return Line.Split(',');
        }

        public static void ParseTexture(List<string> File, string Resource, ref Dictionary<string, Tex> Dict)
        {
            string ItemToFind = TrimExcess(Resource);

            Tex texture = new Tex();
            texture.TexOffset = 0;

            int FileLine = FindResourceLine(File, ItemToFind);

            FileLine++;
            List<UInt64> Values = new List<ulong>();

            while (File[FileLine] != "};")
            {
                //going through function array thing
                string buf = File[FileLine].Trim();
                string[] Items = buf.Split(",");
                for(int i = 0; i < Items.Length - 1; i++)
                {
                    string Clean = Items[i].Replace("0x", "").Trim();
                    UInt64 CurVal = Convert.ToUInt64(Clean, 16);
                    //Console.WriteLine(CurVal);
                    Values.Add(CurVal);
                }
                FileLine++;
            }

            texture.Texture = Values.ToArray();
            texture.Identifier = ItemToFind;

            Dict.Add(ItemToFind, texture);
        }

        private static int FindResourceLine(List<string> File, string ItemToFind)
        {
            int FileLine = -1;
            for (int i = 0; i < File.Count; i++)
            {
                if (File[i].Contains(ItemToFind))
                {
                    FileLine = i; //found line
                    break;
                }                
            }

            if (FileLine == -1)
            {
                throw new Exception("Couldn't find item " + ItemToFind);
            }

            return FileLine;
        }

        public static void ParseVTX(List<string> File, string Resource, ref Dictionary<string, VTX> Dict)
        {
            string ItemToFind = TrimExcess(Resource);
            VTX vert = new VTX();
            vert.VertexOffset = 0;

            int FileLine = FindResourceLine(File, ItemToFind);
            FileLine++;

            List<VTX_Item> Items = new List<VTX_Item>();
            while(File[FileLine] != "};")
            {
                string buf = File[FileLine].Replace("{", "").Trim();
                buf = buf.Replace("}", "");
                string[] data = buf.Split(',');


                VTX_Item Cur = new VTX_Item();
                Cur.Pos = new Vector3(short.Parse(data[0]), short.Parse(data[1]), short.Parse(data[2]));
                Cur.flag = ushort.Parse(data[3]);
                Cur.Coords = new Vector2(short.Parse(data[4]), short.Parse(data[5]));
                Cur.Colours = new Vector4(Convert.ToInt32(data[6].Replace("0x","").Trim(),16), Convert.ToInt32(data[7].Replace("0x", "").Trim(), 16), Convert.ToInt32(data[8].Replace("0x", "").Trim(), 16), Convert.ToInt32(data[9].Replace("0x", "").Trim(), 16));
                //Cur.Colours = new Vector4(0x00, 0x7F, 0x00, 0xFF);

                Items.Add(Cur);
                FileLine++;
            }

            vert.Vertex = Items.ToArray();
            vert.Identifier = ItemToFind;

            Dict.Add(ItemToFind, vert);
        }
        private static string TrimExcess(string Resource)
        {
            string ItemToFind = Resource.Replace("extern ", "");
            ItemToFind = ItemToFind.Substring(0, ItemToFind.IndexOf('['));
            ItemToFind = ItemToFind.Trim(); //remove all unneeded data
            return ItemToFind;
        }

        private static string GetCommand(string FullLine)
        {
            return FullLine.Substring(0, FullLine.IndexOf('(')).Trim();
        }

        private static UInt32 GetGeometryMode(string FullLine)
        {
            UInt32 mode = 0;
            string L = FullLine.ToUpper();

            if(L.Contains("G_ZBUFFER"))
            {
                mode |= 0b00000000000000000000000000000001;
            }
            if(L.Contains("G_SHADE"))
            {
                mode |= 0b00000000000000000000000000000100;
            }
            if (L.Contains("G_CULL_FRONT"))
            {
                mode |= 0b00000000000000000000000100000000;
            }
            if (L.Contains("G_CULL_BACK"))
            {
                mode |= 0b00000000000000000000001000000000;
            }
            if (L.Contains("G_FOG"))
            {
                mode |= 0b00000000000000010000000000000000;
            }
            if (L.Contains("G_LIGHTING"))
            {
                mode |= 0b00000000000000100000000000000000;
            }
            if (L.Contains("G_TEXTURE_GEN"))
            {
                mode |= 0b00000000000001000000000000000000;
            }
            if (L.Contains("G_SHADING_SMOOTH"))
            {
                mode |= 0b00000000000000000000001000000000;
            }
            if (L.Contains("G_CLIPPING"))
            {
                mode |= 0b00000000100000000000000000000000;
            }

            return mode;
        }

        private static UInt64 _SHIFTL(UInt64 no, int Shiftamm, UInt64 Length)
        {
            return ((no & (UInt64)(Math.Pow(2, Length) - 1)) << Shiftamm );
        }

        private static UInt64 GCCc0w0(UInt64 saRGB0, UInt64 mRGB0, UInt64 saA0, UInt64 mA0)
        {
            return (_SHIFTL(saRGB0, 20, 4) | _SHIFTL(mRGB0, 15, 5) | _SHIFTL(saA0, 12, 3) | _SHIFTL(mA0, 9, 3));
        }

        private static UInt64 GCCc1w0(UInt64 saRGB1, UInt64 mRGB1)
        {
            return (_SHIFTL(saRGB1, 5, 4) | _SHIFTL(mRGB1, 0, 5));
        }

        private static UInt64 GCCc0w1(UInt64 sbRGB0, UInt64 aRGB0, UInt64 sbA0, UInt64 aA0)
        {
            return (_SHIFTL(sbRGB0, 28, 4) | _SHIFTL(aRGB0, 15, 3) | _SHIFTL(sbA0, 12, 3) | _SHIFTL(aA0, 9, 3));
        }

        private static UInt64 GCCc1w1(UInt64 sbRGB1, UInt64 saA1, UInt64 mA1, UInt64 aRGB1, UInt64 sbA1, UInt64 aA1)
        {
            return _SHIFTL(sbRGB1, 24, 4) | _SHIFTL(saA1, 21, 3) |
                   _SHIFTL(mA1, 18, 3) | _SHIFTL(aRGB1, 6, 3) |
                   _SHIFTL(sbA1, 3, 3) | _SHIFTL(aA1, 0, 3);
        }

        private static UInt64 gsImmp1(UInt64 c, UInt64 p0)
        {
            return _SHIFTL(c, 24 + 24 + 8, 8) | p0;
        }
    }
}
