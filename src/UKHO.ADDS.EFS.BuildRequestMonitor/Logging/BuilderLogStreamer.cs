﻿using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Logging
{
    internal class BuilderLogStreamer
    {
        private readonly DockerClient _dockerClient;

        public BuilderLogStreamer(DockerClient dockerClient)
        {
            _dockerClient = dockerClient;
        }

        public async Task StreamLogsAsync(string containerId, Action<string> logStdout, Action<string> logStderr, CancellationToken cancellationToken = default)
        {
            var stream = await _dockerClient.Containers.GetContainerLogsAsync(
                containerId,
                false, // false = multiplexed stream with both stdout and stderr
                new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true, Timestamps = false },
                cancellationToken
            );

            await ReadFromMultiplexedStreamAsync(stream, logStdout, logStderr, cancellationToken);
        }

        private static async Task ReadFromMultiplexedStreamAsync(MultiplexedStream stream, Action<string> logStdout, Action<string> logStderr, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);

                if (result.EOF || result.Count == 0)
                {
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);

                switch (result.Target)
                {
                    case MultiplexedStream.TargetStream.StandardOut:
                        logStdout(text);
                        break;
                    case MultiplexedStream.TargetStream.StandardError:
                        logStderr(text);
                        break;
                }
            }
        }
    }
}
