using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

using sfms_rest_api;
using Microsoft.AspNetCore.Http;

namespace sfms_api_test;

[TestClass]
public class SfmsControllerTest
{
    public SfmsControllerTest()
    {
        container = sample.CreateSampleContainer();
        var mockLogger = Mock.Of<ILogger<SfmsController>>();
        controller = new SfmsController(mockLogger, container);
    }
    private readonly SfmsController controller;
    private readonly sfms.Container container;
    private readonly TestSample sample = new TestSample1();

    [TestMethod]
    public void GetVersion()
    {
        var result = controller.GetVersion() as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(200, result.StatusCode);

        var versions = ToDictionary(result.Value);
        Assert.IsNotNull(versions);
        Assert.IsTrue(versions.ContainsKey("assemblyVersion"));
        Assert.IsTrue(versions.ContainsKey("fileVersion"));
        Assert.IsTrue(versions.ContainsKey("productVersion"));
    }

    [TestMethod]
    public async Task GetFiles()
    {
        var result = await controller.GetFiles(TestSample1.TEST_DIR_PATH) as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(200, result.StatusCode);

        var files = result.Value as List<sfms.File>;
        Assert.IsNotNull(files);
        Assert.AreEqual(sample.CountOriginalFiles(TestSample1.TEST_DIR_PATH), files.Count);
    }

    [TestMethod]
    public async Task GetFiles_Empty()
    {
        var result = await controller.GetFiles(TestSample1.EMPTY_DIR_PATH) as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(200, result.StatusCode);

        var files = result.Value as List<sfms.File>;
        Assert.IsNotNull(files);
        Assert.AreEqual(0, files.Count);
    }

    [TestMethod]
    public async Task Delete()
    {
        await Assert.ThrowsExceptionAsync<sfms.NotFoundException>(() =>
            controller.Delete(TestSample1.NOT_EXIST_FILE_PATH)
        );

        ShouldFileExist(TestSample1.TEST_FILE_PATH);

        var result = await controller.Delete(TestSample1.TEST_FILE_PATH) as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(200, result.StatusCode);

        ShouldNotFileExist(TestSample1.TEST_FILE_PATH);
    }

    [TestMethod]
    public async Task Rename()
    {
        ShouldFileExist(TestSample1.TEST_FILE_PATH);
        ShouldNotFileExist(TestSample1.NOT_EXIST_FILE_PATH);

        await Assert.ThrowsExceptionAsync<sfms.NotFoundException>(() =>
            controller.Rename(TestSample1.NOT_EXIST_FILE_PATH, TestSample1.TEST_FILE_PATH)
        );
        await Assert.ThrowsExceptionAsync<sfms.AlreadyExistsException>(() =>
            controller.Rename(TestSample1.TEST_FILE_PATH, TestSample1.TEST_FILE_PATH)
        );

        var result = await controller.Rename(TestSample1.TEST_FILE_PATH, TestSample1.NOT_EXIST_FILE_PATH) as OkObjectResult;
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        var file = GetNotNullValue(result) as sfms.File;
        Assert.IsNotNull(file);
        Assert.AreEqual(TestSample1.NOT_EXIST_FILE_PATH, file.filePath);

        ShouldFileExist(TestSample1.NOT_EXIST_FILE_PATH);
        ShouldNotFileExist(TestSample1.TEST_FILE_PATH);
    }

    [TestMethod]
    public async Task WriteFile()
    {
        await Assert.ThrowsExceptionAsync<sfms.AlreadyExistsException>(() =>
            controller.WriteFile(TestSample1.TEST_FILE_PATH)
        );

        const string TEST_ORIGINAL_FILE_NAME = "test.file.name";
        var TEST_CONTENTS = new byte[] { 0xaa, 0xbb, 0xcc };
        var base64encoded = Convert.ToBase64String(TEST_CONTENTS);

        var result = await controller.WriteFile(TestSample1.NOT_EXIST_FILE_PATH, TEST_ORIGINAL_FILE_NAME, base64encoded) as OkObjectResult;
        var file = GetNotNullValue(result) as sfms.File;
        Assert.IsNotNull(file);
        Assert.AreEqual(TEST_ORIGINAL_FILE_NAME, FileMeta.FromJson(file.meta).OriginalFileName);
        Assert.AreEqual(TEST_CONTENTS.Length, file.originalFileSize);

        var content = container.ReadFile(file);
        Assert.IsNotNull(content);
        Assert.IsTrue(TEST_CONTENTS.SequenceEqual(content.data));
    }

    [TestMethod]
    public async Task UploadFile()
    {
        // https://stackoverflow.com/a/36869339
        var uploadFileMock = Mock.Of<Microsoft.AspNetCore.Http.IFormFile>();

        await Assert.ThrowsExceptionAsync<sfms.AlreadyExistsException>(() =>
            controller.UploadFile(TestSample1.TEST_FILE_PATH, uploadFileMock)
        );

        const string TEST_ORIGINAL_FILE_NAME = "test.file.name";
        var TEST_CONTENTS = new byte[] { 0xaa, 0xbb, 0xcc };
        var stream = new MemoryStream(TEST_CONTENTS);
        var uploadFile = new FormFile(stream, 0, stream.Length, "", TEST_ORIGINAL_FILE_NAME);

        var result = await controller.UploadFile(TestSample1.NOT_EXIST_FILE_PATH, uploadFile) as OkObjectResult;
        var file = GetNotNullValue(result) as sfms.File;
        Assert.IsNotNull(file);
        Assert.AreEqual(TEST_ORIGINAL_FILE_NAME, FileMeta.FromJson(file.meta).OriginalFileName);
        Assert.AreEqual(TEST_CONTENTS.Length, file.originalFileSize);

        var content = container.ReadFile(file);
        Assert.IsNotNull(content);
        Assert.IsTrue(TEST_CONTENTS.SequenceEqual(content.data));
    }

    [TestMethod]
    public async Task ReadFile()
    {
        await Assert.ThrowsExceptionAsync<sfms.NotFoundException>(() =>
            controller.ReadFile(TestSample1.NOT_EXIST_FILE_PATH)
        );

        var originalContent = sample.GetOriginalFileContent(TestSample1.TEST_FILE_PATH);
        Assert.IsNotNull(originalContent);
        var file = container.GetFile(TestSample1.TEST_FILE_PATH);
        Assert.IsNotNull(file);
        Assert.AreEqual(originalContent.Length, file.originalFileSize);
        var originalFileName = FileMeta.FromJson(file.meta).OriginalFileName;

        var result = await controller.ReadFile(TestSample1.TEST_FILE_PATH) as OkObjectResult;
        var content = GetNotNullValue(result) as sfms.FileContent;
        Assert.IsNotNull(content);
        Assert.IsTrue(originalContent.SequenceEqual(content.data));
    }


    [TestMethod]
    public async Task DownloadFile()
    {
        await Assert.ThrowsExceptionAsync<sfms.NotFoundException>(() =>
            controller.DownloadFile(TestSample1.NOT_EXIST_FILE_PATH, false)
        );

        var originalContent = sample.GetOriginalFileContent(TestSample1.TEST_FILE_PATH);
        Assert.IsNotNull(originalContent);
        var file = container.GetFile(TestSample1.TEST_FILE_PATH);
        Assert.IsNotNull(file);
        Assert.AreEqual(originalContent.Length, file.originalFileSize);
        var originalFileName = FileMeta.FromJson(file.meta).OriginalFileName;

        var result = await controller.DownloadFile(TestSample1.TEST_FILE_PATH, true) as FileContentResult;
        Assert.IsNotNull(result);
        Assert.AreEqual(originalFileName, result.FileDownloadName);
        Assert.IsTrue(originalContent.SequenceEqual(result.FileContents));
    }



    #region internal helpers

    private static object GetNotNullValue(ObjectResult? objectResult)
    {
        Assert.IsNotNull(objectResult);
        var value = objectResult.Value;
        Assert.IsNotNull(value);
        return value;
    }

    private void ShouldFileExist(string filePath)
    {
        Assert.IsNotNull(container.GetFile(filePath));
    }

    private void ShouldNotFileExist(string filePath)
    {
        Assert.IsNull(container.GetFile(filePath));
    }

    private static Dictionary<string, string?> ToDictionary(object ananymous)
    {
        var props = ananymous.GetType().GetProperties();
        return props.ToDictionary(p => p.Name, p => p.GetValue(ananymous, null)?.ToString());
    }

    #endregion
}