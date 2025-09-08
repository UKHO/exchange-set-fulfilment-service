using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Enums;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Interfaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Models;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging
{
    /// <summary>
    ///     The Azure storage event logger model
    /// </summary>
    //public class AzureStorageEventLogger : IAzureStorageEventLogger, IDisposable
    //{
    //    private readonly BlobContainerClient _containerClient;
    //    private CancellationToken _cancellationToken;
    //    private CancellationTokenSource _cancellationTokenSource = new();
    //    private bool _disposed;

    //    /// <summary>
    //    ///     Constructor
    //    /// </summary>
    //    /// <param name="containerClient">The container client</param>
    //    public AzureStorageEventLogger(BlobContainerClient containerClient)
    //    {
    //        _containerClient = containerClient;
    //        _cancellationToken = new CancellationToken();
    //    }

    //    /// <summary>
    //    ///     Generates the service name for the container subfolder
    //    /// </summary>
    //    /// <param name="serviceName">The service name</param>
    //    /// <param name="environment">The environment</param>
    //    /// <returns>the service name(string)</returns>
    //    //public string GenerateServiceName(string serviceName, string environment)
    //    //{
    //    //    return string.Format("{0} - {1}", serviceName, environment);
    //    //}

    //    /// <summary>
    //    ///     Generates a path for the error message blob
    //    /// </summary>
    //    /// <param name="date">The datetime</param>
    //    /// <returns>The blob path</returns>
    //    //public string GeneratePathForErrorBlob(DateTime date)
    //    //{
    //    //    return Path.Combine(date.Year.ToString(), date.Month.ToString(), date.Day.ToString(), date.Hour.ToString(), date.Minute.ToString(), date.Second.ToString());
    //    //}

    //    /// <summary>
    //    ///     Generates a new blob name
    //    /// </summary>
    //    /// <param name="name">The name of the blob</param>
    //    /// <param name="extension">The extension</param>
    //    /// <returns>The blob name(with extension)</returns>
    //    //public string GenerateErrorBlobName(Guid? name = null, string extension = null)
    //    //{
    //    //    string blobName;
    //    //    if (name == null)
    //    //        blobName = Guid.NewGuid().ToString().Replace("-", "_");
    //    //    else
    //    //        blobName = name.ToString().Replace("-", "_");

    //    //    if (string.IsNullOrEmpty(extension))
    //    //        blobName += string.Format("{0}{1}", ".", "json");
    //    //    else
    //    //        blobName += string.Format("{0}{1}", ".", extension);

    //    //    return blobName;
    //    //}

    //    /// <summary>
    //    ///     Generates the full name for the blob
    //    /// </summary>
    //    /// <param name="storageBlobFullNameModel">The blob full name model</param>
    //    /// <returns>The fullname of the blob (path + name)</returns>
    //    //public string GenerateBlobFullName(AzureStorageBlobFullNameModel storageBlobFullNameModel)
    //    //{
    //    //    return Path.Combine(storageBlobFullNameModel.ServiceName, storageBlobFullNameModel.Path, storageBlobFullNameModel.BlobName);
    //    //}

    //    /// <summary>
    //    ///     Cancels the storing operation (when possible)
    //    /// </summary>
    //    /// <returns>AzureStorageEventLogCancellationResult</returns>
    //    //public AzureStorageEventLogCancellationResult CancelLogFileStoringOperation()
    //    //{
    //    //    if (_cancellationToken.CanBeCanceled)
    //    //        try
    //    //        {
    //    //            _cancellationTokenSource.Cancel();
    //    //            return AzureStorageEventLogCancellationResult.Successful;
    //    //        }
    //    //        catch (Exception)
    //    //        {
    //    //            return AzureStorageEventLogCancellationResult.CancellationFailed;
    //    //        }

    //    //    return AzureStorageEventLogCancellationResult.UnableToCancel;
    //    //}

    //    /// <summary>
    //    ///     Stores the log file on Azure
    //    /// </summary>
    //    /// <param name="model">The azure storage event model</param>
    //    /// <param name="withCancellation">Flag for the cancellation token</param>
    //    /// <returns>The azure storage log result</returns>
    //    //public AzureStorageEventLogResult StoreLogFile(AzureStorageEventModel model, bool withCancellation = false)
    //    //{
    //    //    var isStored = false;
    //    //    var binaryData = new BinaryData(model.Data);

    //    //    if (withCancellation)
    //    //        _cancellationToken = _cancellationTokenSource.Token;

    //    //    Response<BlobContentInfo> uploadBlobResponse;
    //    //    try
    //    //    {
    //    //        uploadBlobResponse = _containerClient.UploadBlob(model.FileFullName, binaryData, _cancellationToken);
    //    //    }
    //    //    catch (Exception uploadBlobException)
    //    //    {
    //    //        return new AzureStorageEventLogResult(string.Format("{0}-{1}", uploadBlobException.GetType().Name, uploadBlobException.Message),
    //    //                                              0,
    //    //                                              Guid.Empty.ToString(),
    //    //                                              null,
    //    //                                              false,
    //    //                                              model.FileFullName);
    //    //    }

    //    //    if (uploadBlobResponse.Value != null
    //    //        && uploadBlobResponse.GetRawResponse().Status == (int)HttpStatusCode.Created
    //    //        && uploadBlobResponse.GetRawResponse().ReasonPhrase == HttpStatusCode.Created.ToString())
    //    //            isStored = true;

    //    //    return new AzureStorageEventLogResult(uploadBlobResponse.GetRawResponse().ReasonPhrase,
    //    //                                          uploadBlobResponse.GetRawResponse().Status,
    //    //                                          uploadBlobResponse.GetRawResponse().ClientRequestId,
    //    //                                          uploadBlobResponse.Value.EncryptionKeySha256,
    //    //                                          isStored,
    //    //                                          model.FileFullName,
    //    //                                          uploadBlobResponse.GetRawResponse().GetFileSize(model.Data.Length),
    //    //                                          uploadBlobResponse.GetRawResponse().GetModifiedDate());
    //    //}

    //    /// <summary>
    //    ///     Stores the log file on Azure(Async)
    //    /// </summary>
    //    /// <param name="model">The azure storage event model</param>
    //    /// <param name="withCancellation">Flag for the cancellation token</param>
    //    /// <returns>The azure storage log result(Task)</returns>
    //    //public async Task<AzureStorageEventLogResult> StoreLogFileAsync(AzureStorageEventModel model, bool withCancellation = false)
    //    //{
    //    //    var isStored = false;
    //    //    var binaryData = new BinaryData(model.Data);

    //    //    if (withCancellation)
    //    //        _cancellationToken = _cancellationTokenSource.Token;

    //    //    Response<BlobContentInfo> uploadBlobResponse;
    //    //    try
    //    //    {
    //    //        uploadBlobResponse = await _containerClient.UploadBlobAsync(model.FileFullName, binaryData, _cancellationToken);
    //    //    }
    //    //    catch (Exception uploadBlobException)
    //    //    {
    //    //        return new AzureStorageEventLogResult(string.Format("{0}-{1}", uploadBlobException.GetType().Name, uploadBlobException.Message),
    //    //                                              0,
    //    //                                              Guid.Empty.ToString(),
    //    //                                              null,
    //    //                                              false,
    //    //                                              model.FileFullName);
    //    //    }

    //    //    if (uploadBlobResponse.Value != null
    //    //        && uploadBlobResponse.GetRawResponse().Status == (int)HttpStatusCode.Created
    //    //            && uploadBlobResponse.GetRawResponse().ReasonPhrase == HttpStatusCode.Created.ToString())
    //    //            isStored = true;

    //    //    return new AzureStorageEventLogResult(uploadBlobResponse.GetRawResponse().ReasonPhrase,
    //    //                                          uploadBlobResponse.GetRawResponse().Status,
    //    //                                          uploadBlobResponse.GetRawResponse().ClientRequestId,
    //    //                                          uploadBlobResponse.Value.EncryptionKeySha256,
    //    //                                          isStored,
    //    //                                          model.FileFullName,
    //    //                                          uploadBlobResponse.GetRawResponse().GetFileSize(model.Data.Length),
    //    //                                          uploadBlobResponse.GetRawResponse().GetModifiedDate());
    //    //}

    //    /// <summary>
    //    ///     Nullify the token source (For Unit tests only)
    //    /// </summary>
    //    //public void NullifyTokenSource()
    //    //{
    //    //    _cancellationTokenSource = null;
    //    //}
    //    ///// <summary>
    //    /////     Disposes the cancellation token source.
    //    ///// </summary>
    //    //public void Dispose()
    //    //{
    //    //    Dispose(true);
    //    //    GC.SuppressFinalize(this);
    //    //}

    //    //protected virtual void Dispose(bool disposing)
    //    //{
    //    //    if (_disposed)
    //    //        return;

    //    //    if (disposing)
    //    //    {
    //    //        _cancellationTokenSource?.Dispose();
    //    //        _cancellationTokenSource = null;
    //    //    }

    //    //    _disposed = true;
    //    //}

    //}
}
