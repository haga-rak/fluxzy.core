using System;
using Echoes.Encoding.HPack;
using Xunit;

namespace Echoes.Encoding.Tests
{
    public class BinaryIoInt32Tests
    {
        [Fact]
        public void Write_And_Read_Lt_2N_1()
        {
            Span<byte> buffer = stackalloc byte [8] ;
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;
            int writeValue = 2; 

            int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
            
        }

        [Fact]
        public void Write_And_Read_Gt_2N_1()
        {
            Span<byte> buffer = stackalloc byte [8 ];
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;
            int writeValue = 66; 

            int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
            
        }

        [Fact]
        public void Write_And_Read_Limit()
        {
            Span<byte> buffer = stackalloc byte [8];
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;
            int writeValue = (1 << prefixSize) - 1 ; 

            int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
            
        }

        [Fact]
        public void Write_And_Read_Limit_2N_1_plus_0x7F()
        {
            Span<byte> buffer = stackalloc byte [8];
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;
            int writeValue = 190 ; 

            int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
            
        }

        [Fact]
        public void Write_And_Read_Limit_2N_1_plus_0x7F_plus_1()
        {
            Span<byte> buffer = stackalloc byte [8 ];
            var binaryHelper = new PrimitiveOperation();
            int prefixSize = 6;
            int writeValue = 191 ; 

            int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
        }

        [Fact]
        public void Write_And_Read_Until_2N_16()
        {
            Span<byte> buffer = stackalloc byte [8];
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;

            for (int i = 1; i < (1 << 16); i++)
            {
                int writeValue = i;

                int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

               // buffer.Clear();

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Fact]
        public void Write_And_Read_Until_2N_16_Prefix_5()
        {
            Span<byte> buffer = stackalloc byte [8];
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 5;

            for (int i = 1; i < (1 << 16); i++)
            {
                int writeValue = i;

                int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Fact]
        public void Write_And_Read_Until_2N_16_Prefix_7()
        {
            Span<byte> buffer = stackalloc byte [8] ;
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 1;

            for (int i = 1; i < (1 << 16); i++)
            {
                int writeValue = i;

                int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

               // buffer.Clear();

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Fact]
        public void Write_And_Read_Every_Limit()
        {
            Span<byte> buffer = stackalloc byte [8];
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;
            
            for (int i = 1; i < 27; i++)
            {
                int writeValue = (1 << i) - 1 ;

                int offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                int offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

               // buffer.Clear();

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Fact]
        public void Write_And_Read_With_Error()
        {
            var binaryHelper = new PrimitiveOperation();

            int prefixSize = 6;
            int writeValue = 66;

            Assert.Throws<HPackCodecException>(() =>
            {
                Span<byte> buffer = stackalloc byte[1];
                return binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            }); 
           
        }
    }
}