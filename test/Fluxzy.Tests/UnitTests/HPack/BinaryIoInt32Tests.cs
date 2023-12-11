// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Encoder.HPack;
using Xunit;

namespace Fluxzy.Tests.UnitTests.HPack
{
    public class BinaryIoInt32Tests
    {
        [Theory]
        [InlineData(6)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Lt_2N_1(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            
            var writeValue = 2;

            var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Gt_2N_1(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            
            var writeValue = 66;

            var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Limit(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            
            var writeValue = (1 << prefixSize) - 1;

            var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Limit_2N_1_plus_0x7F(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();

            var writeValue = 190;

            var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Limit_2N_1_plus_0x7F_plus_1(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            var writeValue = 191;

            var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

            Assert.Equal(offsetRead, offsetWrite);
            Assert.Equal(readValue, writeValue);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Until_2N_16(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            

            for (var i = 1; i < 1 << 16; i++)
            {
                var writeValue = i;

                var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

                // buffer.Clear();

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Theory]
        [InlineData(6)]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Until_2N_16_Prefix_5(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            

            for (var i = 1; i < 1 << 16; i++)
            {
                var writeValue = i;

                var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Fact]
        public void Write_And_Read_Until_2N_16_Prefix_7()
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();

            var prefixSize = 1;

            for (var i = 1; i < 1 << 16; i++)
            {
                var writeValue = i;

                var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Theory]
        [InlineData(6)]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_Every_Limit(int prefixSize)
        {
            Span<byte> buffer = stackalloc byte[8];
            var binaryHelper = new PrimitiveOperation();
            
            for (var i = 1; i < 27; i++)
            {
                var writeValue = (1 << i) - 1;

                var offsetWrite = binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
                var offsetRead = binaryHelper.ReadInt32(buffer, prefixSize, out var readValue);

                Assert.Equal(offsetRead, offsetWrite);
                Assert.Equal(readValue, writeValue);
            }
        }

        [Theory]
        [InlineData(6)]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(0)]
        public void Write_And_Read_With_Error(int prefixSize)
        {
            var binaryHelper = new PrimitiveOperation();
            
            var writeValue = 66;

            Assert.Throws<HPackCodecException>(() =>
            {
                Span<byte> buffer = stackalloc byte[1];

                return binaryHelper.WriteInt32(buffer, writeValue, prefixSize);
            });
        }
    }
}
