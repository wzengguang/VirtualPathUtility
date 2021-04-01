//------------------------------------------------------------------------------
// <copyright file="SimpleBitVector32.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Web.Util
{
    using System;

    //
    // This is a cut down copy of System.Collections.Specialized.BitVector32. The
    // reason this is here is because it is used rather intensively by Control and
    // WebControl. As a result, being able to inline this operations results in a
    // measurable performance gain, at the expense of some maintainability.
    //
    /// <summary>
    /// copy from .net 4.8 System.Web.Util
    /// </summary>
    [Serializable]
    internal struct SimpleBitVector32
    {
        private int data;

        internal SimpleBitVector32(int data)
        {
            this.data = data;
        }

        internal int IntegerValue
        {
            get { return data; }
            set { data = value; }
        }

        internal bool this[int bit]
        {
            get
            {
                return (data & bit) == bit;
            }
            set
            {
                int _data = data;
                if (value)
                {
                    data = _data | bit;
                }
                else
                {
                    data = _data & ~bit;
                }
            }
        }

        // Stores and retrieves a positive integer in the bit vector.  The "mask" parameter selects the bits
        // to use in the vector, and the "offset" parameter is the index of the rightmost bit in the
        // mask.  The offset could be calculated from the mask, but is passed in as a separate constant
        // for improved performance.  NOTE: Because the data field is a signed integer, only 31 of the 32
        // available bits may be used.
        // Example: To store a 4-bit integer in bits 4-7, use mask=0x000000F0 and offset=4.
        internal int this[int mask, int offset]
        {
            get
            {        
                return ((data & mask) >> offset);
            }
            set
            {
                data = (data & ~mask) | (value << offset);
            }
        }

        internal void Set(int bit)
        {
            data |= bit;
        }

        internal void Clear(int bit)
        {
            data &= ~bit;
        }
    }
}