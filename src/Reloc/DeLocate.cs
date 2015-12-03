﻿// Shane.Macaulay @IOActive.com Copyright (C) 2013-2015

//Copyright(C) 2015 Shane Macaulay

//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

// Shane.Macaulay@IOActive.com (c) copyright 2014,2015 all rights reserved. GNU GPL License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Reloc
{
    public class Reloc
    {
        public UInt32 PageRVA;
        public Int32 BlockSize;
        public UInt16[] Area;
    }

    /// <summary>
    /// DeLocate provides methods to transform a relocated PE from it's in-memory state
    /// back into it's disk state.  
    /// 
    /// We handle missing pages so in the event you cant fault the binary into memory at runtime
    /// (like the case where your analyzing a memory dump) there will be pleanty of missing pages.
    /// This is why the Delocate routine is so hairy, if there is a missing page or not some state
    /// has to be maintained for instructions which straddle a page boundry.
    /// </summary>
    public class DeLocate
    {
        /// <summary>
        /// TODO: Finish example/integration with other tools
        /// </summary>
#if FALSE
        public string DeLocateFile(string fPath, string RelocFile)
        {
            using (var fs = new FileStream(fPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                var reLocbaseName = Path.GetFileNameWithoutExtension(RelocFile);
            }
        }
#endif


        ulong OverHang;
        bool CarryOne;
        int OvrOffset;
        byte b;

        /// <summary>
        /// This routine takes a binary .reloc and emit's List of type Reloc
        /// 
        /// There are theorietically some .reloc entries we do not support, I've not seen too many for recent binaries.
        /// 
        /// If we wanted to support more, adding more translations here would be fine.
        /// </summary>
        /// <param name="FileBuff"></param>
        /// <returns></returns>
        public static List<Reloc> ProcessRelocs(byte[] FileBuff)
        {
            var rv = new List<Reloc>();

            using (var ms = new MemoryStream(FileBuff))
            using (var reReader = new BinaryReader(ms))
            {
                var pageRVA = reReader.ReadUInt32();
                var blockSize = reReader.ReadInt32();
                var BlockPos = ms.Position;

                var Count = (blockSize - 8) / 2;

                while (blockSize != 0)
                {
                    var rl = new Reloc { PageRVA = pageRVA, BlockSize = blockSize, Area = new ushort[Count] };
                    for (int i = 0; i < Count; i++)
                        rl.Area[i] = reReader.ReadUInt16();

                    rv.Add(rl);

                    pageRVA = reReader.ReadUInt32();
                    if (pageRVA == 0)
                        break;
                    blockSize = reReader.ReadInt32();
                    if (blockSize == 0)
                        break;

                    Count = (blockSize - 8) / 2;
                }
            }
            return rv;
        }

        /// <summary>
        /// I ported this from a C function and will likely write it in safe/C# eventually ;)
        /// Most of my code is rewrites of earlier native stuff I've done since it's nice to have a sandbox to play in.
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="Delta"></param>
        /// <param name="VA"></param>
        /// <param name="relocs"></param>
        public unsafe void DeLocateBuff64(byte* bp, ulong Delta, ulong VA, Reloc[] relocs)
        {
            // round down to page alignment
            var xVA = VA & ~4095UL;

            byte* basep;

            if (relocs == null)
                return;

            for (int i = 0; i < relocs.Length; i++)
            {
                if (relocs[i].PageRVA == xVA)
                {
                    // ANY OVERHANG FROM (LAST-VA == VA-4096), use, otherwise ignore
                    if (OverHang != 0 && (xVA - 4096) == OverHang)
                    {
                        var _3bp = bp;

                        // have only written 1 byte in the previous page
                        switch (OvrOffset)
                        {
                            case 1:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF00) >> 8));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                            case 2:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                            case 3:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                            case 4:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                            case 5:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                            case 6:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                            case 7:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                *_3bp = (byte)(b - ((Delta & 0xFF00000000000000) >> 56));
                                break;
                        }

                        // reset overhang
                        OverHang = 0;
                        CarryOne = false;
                    }

                    for (int j = 0; j < relocs[i].Area.Length; j++)
                    {
                        // their can be a trailing null entry sometimes
                        if (relocs[i].Area[j] == 0)
                            continue;

                        // 4KB max limit
                        var offset = (relocs[i].Area[j]) & 0xFFFUL;

                        // trim offset if we are unaligned reading
                        if (VA != xVA)
                        {
                            var Unaligned = VA - xVA;

                            // this reloc entry is for an earlier/unaligned page
                            if (offset < Unaligned)
                                continue;

                            offset -= Unaligned;
                        }

                        // reset to base pointer every fixup
                        basep = bp;

                        // get byte offset
                        basep += offset;

                        // WRITE 8
                        if (offset < 4089)
                        {
                            // get int* to byte offset
                            var intp = (ulong*)basep;

                            var curr = *intp;

                            // this has got to be an error
                            //if(curr == 0)
                            //Console::WriteLine("B00G");

                            *intp = curr - Delta;
                            OvrOffset = 0;

                        }
                        else {
                            var _3bp = basep;

                            OverHang = xVA;
                            OvrOffset = (int)(4096 - offset);

                            // WRITE 7
                            switch (offset)
                            {
                                case 4089:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                        *_3bp = (byte)(b - (Delta & 0x000000FF));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF000000000000) >> 48) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF000000000000) >> 48));
                                        break;
                                    }                                // WRITE 6
                                case 4090:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                        *_3bp = (byte)(b - (Delta & 0x000000FF));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;

                                        *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF0000000000) >> 40) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF0000000000) >> 40));
                                        break;
                                    }                                // WRITE 5
                                case 4091:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                        *_3bp = (byte)(b - (Delta & 0x000000FF));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;

                                        *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF00000000) >> 32) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF00000000) >> 32));
                                        break;
                                    }                                // WRITE 4
                                case 4092:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                        *_3bp = (byte)(b - (Delta & 0x000000FF));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;

                                        *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                        break;
                                    }                                // WRITE 3
                                case 4093:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                        *_3bp = (byte)(b - (Delta & 0x000000FF));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;

                                        *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                        *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                        break;
                                    }                                // WRITE 2
                                case 4094:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                        *_3bp = (byte)(b - (Delta & 0x000000FF));
                                        _3bp++;

                                        if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                        CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;

                                        *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                        _3bp++;
                                        break;
                                    }                                // WRITE 1
                                case 4095:
                                    {
                                        b = *_3bp;
                                        CarryOne = b < (Delta & 0x000000FF) ? true : false;

                                        *_3bp = (byte)(b - ((Delta & 0x000000FF)));
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }
        public unsafe void DeLocateBuff32(byte* bp, uint Delta, uint VA, Reloc[] relocs)
        {
            // round down to page alignment
            var xVA = VA & ~4095u;

            byte* basep;

            if (relocs == null)
                return;

            for (int i = 0; i < relocs.Length; i++)
            {
                if (relocs[i].PageRVA == xVA)
                {
                    // ANY OVERHANG FROM (LAST-VA == VA-4096), use, otherwise ignore
                    if (OverHang != 0 && (xVA - 4096) == OverHang)
                    {
                        var _3bp = bp;

                        // have only written 1 byte in the previous page
                        switch (OvrOffset)
                        {
                            case 1:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF00) >> 8));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                break;
                            case 2:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                _3bp++;
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                break;
                            case 3:
                                if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                CarryOne = b < ((Delta & 0xFF000000) >> 24) ? true : false;
                                *_3bp = (byte)(b - ((Delta & 0xFF000000) >> 24));
                                break;
                            default:
                                break;
                        }
                        // reset overhang
                        OverHang = 0;
                        CarryOne = false;
                    }

                    for (int j = 0; j < relocs[i].Area.Length; j++)
                    {
                        // their can be a trailing null entry sometimes
                        if (relocs[i].Area[j] == 0)
                            continue;

                        // 4KB max limit
                        var offset = (relocs[i].Area[j]) & 0xFFFu;

                        // trim offset if we are unaligned reading
                        if (VA != xVA)
                        {
                            var Unaligned = VA - xVA;

                            // this reloc entry is for an earlier/unaligned page
                            if (offset < Unaligned)
                                continue;

                            offset -= Unaligned;
                        }

                        // reset to base pointer every fixup
                        basep = bp;

                        // get byte offset
                        basep += offset;

                        // WRITE 8
                        if (offset < 4089)
                        {
                            // get int* to byte offset
                            var intp = (uint*)basep;

                            var curr = *intp;

                            // this has got to be an error
                            //if(curr == 0)
                            //Console::WriteLine("B00G");

                            *intp = curr - Delta;
                            OvrOffset = 0;

                        }
                        else {
                            var _3bp = basep;

                            OverHang = xVA;
                            OvrOffset = (int)(4096 - offset);

                            // WRITE 7
                            switch (offset)
                            {
                                // WRITE 3
                                case 4093:
                                {
                                    b = *_3bp;
                                    CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                    *_3bp = (byte)(b - (Delta & 0x000000FF));
                                    _3bp++;
                                    if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                    CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;
                                    *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                    _3bp++;
                                    if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                    CarryOne = b < ((Delta & 0x00FF0000) >> 16) ? true : false;
                                    *_3bp = (byte)(b - ((Delta & 0x00FF0000) >> 16));
                                    break;
                                }                                // WRITE 2
                                case 4094:
                                {
                                    b = *_3bp;
                                    CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                    *_3bp = (byte)(b - (Delta & 0x000000FF));
                                    _3bp++;
                                    if (CarryOne) b = (byte)(*_3bp - 1); else b = *_3bp;
                                    CarryOne = b < ((Delta & 0x0000FF00) >> 8) ? true : false;
                                    *_3bp = (byte)(b - ((Delta & 0x0000FF00) >> 8));
                                    _3bp++;
                                    break;
                                }                                // WRITE 1
                                case 4095:
                                {
                                    b = *_3bp;
                                    CarryOne = b < (Delta & 0x000000FF) ? true : false;
                                    *_3bp = (byte)(b - ((Delta & 0x000000FF)));
                                    break;
                                }
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}