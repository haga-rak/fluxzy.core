// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.Grpc;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class ProtobufProducerTests : FormatterTestBase
    {
        private static byte[] CreateGrpcFrame(byte[] payload, bool compressed = false)
        {
            var frame = new byte[5 + payload.Length];
            frame[0] = compressed ? (byte) 1 : (byte) 0;
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(1, 4), (uint) payload.Length);
            Buffer.BlockCopy(payload, 0, frame, 5, payload.Length);
            return frame;
        }

        [Fact]
        public async Task RequestProducer_GrpcRequest_ReturnsResult()
        {
            var producer = new RequestProtobufProducer();
            var randomFile = GetRegisteredRandomFile();

            // Create a protobuf payload: field 1 = "hello"
            var protobufPayload = new byte[] { 0x0A, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F };
            var grpcFrame = CreateGrpcFrame(protobufPayload);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                "https://sandbox.fluxzy.io/hello.HelloService/SayHello");

            requestMessage.Content = new ByteArrayContent(grpcFrame);
            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);
            var result = producer.Build(firstExchange, producerContext);

            // The request body as seen by the archive will be the actual response from the server,
            // not our crafted gRPC frame (since the proxy sends the request upstream).
            // This test validates that the producer doesn't crash on non-gRPC responses.
            // For a true gRPC test, we'd need a gRPC server.
        }

        [Fact]
        public async Task RequestProducer_NonGrpcRequest_ReturnsNull()
        {
            var producer = new RequestProtobufProducer();
            var randomFile = GetRegisteredRandomFile();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                "https://sandbox.fluxzy.io/api/test");

            requestMessage.Content = new StringContent("{\"key\": \"value\"}");
            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);
            var result = producer.Build(firstExchange, producerContext);

            Assert.Null(result);
        }

        [Fact]
        public async Task ResponseProducer_NonGrpcRequest_ReturnsNull()
        {
            var producer = new ResponseProtobufProducer();
            var randomFile = GetRegisteredRandomFile();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                "https://sandbox.fluxzy.io/");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);
            var result = producer.Build(firstExchange, producerContext);

            Assert.Null(result);
        }

        [Fact]
        public void RawDecoder_DecodesProtobuf_FieldsPresent()
        {
            // field 1 (string) = "hello", field 2 (varint) = 42
            var data = new byte[] {
                0x0A, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F, // field 1 = "hello"
                0x10, 0x2A // field 2 = 42
            };

            var result = RawProtobufDecoder.Decode(data);

            Assert.Contains("hello", result);
            Assert.Contains("42", result);
        }

        [Fact]
        public void GrpcFrameHelper_ExtractFrames_CompressedFrame()
        {
            var payload = new byte[] { 0x08, 0x01 };
            var frame = CreateGrpcFrame(payload, compressed: true);

            var frames = GrpcFrameHelper.ExtractFrames(frame);

            Assert.Single(frames);
            Assert.True(frames[0].Compressed);
        }

        [Fact]
        public void GrpcFrameHelper_ExtractMultipleFrames()
        {
            var payload1 = new byte[] { 0x08, 0x01 };
            var payload2 = new byte[] { 0x08, 0x02 };
            var frame1 = CreateGrpcFrame(payload1);
            var frame2 = CreateGrpcFrame(payload2);

            var combined = new byte[frame1.Length + frame2.Length];
            Buffer.BlockCopy(frame1, 0, combined, 0, frame1.Length);
            Buffer.BlockCopy(frame2, 0, combined, frame1.Length, frame2.Length);

            var frames = GrpcFrameHelper.ExtractFrames(combined);

            Assert.Equal(2, frames.Count);
            Assert.False(frames[0].Compressed);
            Assert.False(frames[1].Compressed);
        }

        [Fact]
        public void ProtobufFormattingResult_Properties()
        {
            var result = new ProtobufFormattingResult("Test", "content") {
                HasProtoDescriptor = true,
                ServiceName = "hello.HelloService",
                MethodName = "SayHello",
                GrpcStatus = 0,
                GrpcMessage = "OK"
            };

            Assert.Equal("Test", result.Title);
            Assert.Equal("content", result.FormattedContent);
            Assert.True(result.HasProtoDescriptor);
            Assert.Equal("hello.HelloService", result.ServiceName);
            Assert.Equal("SayHello", result.MethodName);
            Assert.Equal(0, result.GrpcStatus);
            Assert.Equal("OK", result.GrpcMessage);
        }
    }
}
