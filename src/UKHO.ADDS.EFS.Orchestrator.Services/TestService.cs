using System.Runtime.InteropServices;
using Docker.DotNet.Models;
using Docker.DotNet;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class TestService
    {
        


        public async Task TestMethod()
        {
            var dt = DateTime.Now;

            const string imageName = "ukhoaddsefsbuilder:dev";
            const string tag = "latest";
            const string fullImageName = $"{imageName}:{tag}";
            string containerName = $"my-container-{dt.Hour}-{dt.Minute}-{dt.Second}";


            try
            {
                var dockerClient = new DockerClientConfiguration(LocalDockerUri()).CreateClient();

                //// Pull the latest image
                //await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
                //{
                //    FromImage = imageName,
                //    //Tag = tag
                //}, new AuthConfig(), new Progress<JSONMessage>());

                //// Remove any stopped container with the same name
                //var existingContainers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                //var existing = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));
                //if (existing != null)
                //{
                //    if (!existing.State.Equals("running", StringComparison.OrdinalIgnoreCase))
                //    {
                //        await dockerClient.Containers.RemoveContainerAsync(existing.ID, new ContainerRemoveParameters { Force = true });
                //    }
                //    else
                //    {
                //        Console.WriteLine("Container already running.");
                //        return;
                //    }
                //}

                // Create and start the container
                var response = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = "ukhoaddsefsbuilder:dev",
                    Name = containerName,
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "80/tcp", new List<PortBinding> { new PortBinding { HostPort = "8080" } } }
                        }
                    }
                });

                await dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

                Console.WriteLine($"Started container {containerName} from image {fullImageName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static Uri LocalDockerUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            return isWindows ? new Uri("npipe://./pipe/docker_engine") : new Uri("unix:/var/run/docker.sock");
        }
    }
}
