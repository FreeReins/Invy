using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using SkiaSharp;

namespace Invy.Core.Tests;

public class AttachmentTests
{
    private static readonly byte[] Pdf = "%PDF-1.7\n%fake\n"u8.ToArray();

    [Test]
    public async Task A_photo_is_stored_by_its_hash_with_an_event_and_a_thumbnail_to_make()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");
        var manager = space.Core.AttachmentManager();

        var added = await manager.AddAttachmentAsync(space.Owen, space.SpaceId, drill.Id, new AddAttachmentRequest("C:\\photos\\drill.png", Png(1000, 500)), default);

        await Assert.That(added.Kind).IsEqualTo(AttachmentKind.Photo);
        await Assert.That(added.ContentType).IsEqualTo("image/png");
        await Assert.That(added.Name).IsEqualTo("drill.png");
        var row = space.Core.Attachments.Rows.Single();
        await Assert.That(row.Path).IsEqualTo($"{space.SpaceId:D}/{row.Sha256}.png");
        await Assert.That(space.Core.Files.Files.ContainsKey(row.Path)).IsTrue();
        await Assert.That(space.Core.Items.Events.Count(e => e.ItemId == drill.Id && e.Kind == ItemEventKind.PhotoAdded)).IsEqualTo(1);
        await Assert.That(space.Core.Messages.Published.OfType<MakeThumbnail>().Single().AttachmentId).IsEqualTo(added.Id);
    }

    [Test]
    public async Task The_thumbnail_is_a_small_jpeg_and_making_it_twice_changes_nothing()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");
        var manager = space.Core.AttachmentManager();
        var added = await manager.AddAttachmentAsync(space.Owen, space.SpaceId, drill.Id, new AddAttachmentRequest("drill.png", Png(1000, 500)), default);
        var command = new MakeThumbnail(space.SpaceId, added.Id);

        await manager.MakeThumbnailAsync(command, default);
        await manager.MakeThumbnailAsync(command, default);

        var thumb = await manager.OpenAttachmentAsync(space.Vera, space.SpaceId, added.Id, thumbnail: true, default);
        using var bitmap = SKBitmap.Decode(thumb.Content);
        await Assert.That(thumb.ContentType).IsEqualTo("image/jpeg");
        await Assert.That(bitmap.Width).IsEqualTo(480);
        await Assert.That(bitmap.Height).IsEqualTo(240);
        var listed = await manager.ListAttachmentsAsync(space.Vera, space.SpaceId, drill.Id, default);
        await Assert.That(listed.Single().HasThumbnail).IsTrue();
    }

    [Test]
    public async Task A_pdf_is_a_document_without_a_thumbnail()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");

        var added = await space.Core.AttachmentManager().AddAttachmentAsync(space.Owen, space.SpaceId, drill.Id, new AddAttachmentRequest("", Pdf), default);

        await Assert.That(added.Kind).IsEqualTo(AttachmentKind.Document);
        await Assert.That(added.Name).IsEqualTo("document.pdf");
        await Assert.That(space.Core.Messages.Published.OfType<MakeThumbnail>().Count()).IsEqualTo(0);
        await Assert.That(space.Core.Items.Events.Count(e => e.Kind == ItemEventKind.PhotoAdded)).IsEqualTo(0);
    }

    [Test]
    public async Task Other_files_and_viewers_are_refused()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");
        var manager = space.Core.AttachmentManager();

        var html = await TestData.Caught<RuleViolationException>(
            () => manager.AddAttachmentAsync(space.Owen, space.SpaceId, drill.Id, new AddAttachmentRequest("x.html", "<script>"u8.ToArray()), default));
        var viewer = await TestData.Caught<NotAllowedException>(
            () => manager.AddAttachmentAsync(space.Vera, space.SpaceId, drill.Id, new AddAttachmentRequest("a.pdf", Pdf), default));

        await Assert.That(html?.Message).Contains("photos (JPEG, PNG, WebP or GIF) and PDFs");
        await Assert.That(viewer).IsNotNull();
        await Assert.That(space.Core.Files.Files.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Removing_one_of_two_copies_keeps_the_file_until_the_last_goes()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");
        var saw = await space.ThingAsync("Saw");
        var manager = space.Core.AttachmentManager();
        var first = await manager.AddAttachmentAsync(space.Owen, space.SpaceId, drill.Id, new AddAttachmentRequest("manual.pdf", Pdf), default);
        var second = await manager.AddAttachmentAsync(space.Owen, space.SpaceId, saw.Id, new AddAttachmentRequest("manual.pdf", Pdf), default);

        await manager.DeleteAttachmentAsync(space.Owen, space.SpaceId, first.Id, default);
        var afterFirst = space.Core.Files.Files.Count;
        await manager.DeleteAttachmentAsync(space.Owen, space.SpaceId, second.Id, default);

        await Assert.That(afterFirst).IsEqualTo(1);
        await Assert.That(space.Core.Files.Files.Count).IsEqualTo(0);
        await Assert.That(space.Core.Attachments.Rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Deleting_a_space_removes_its_files()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");
        await space.Core.AttachmentManager().AddAttachmentAsync(space.Owen, space.SpaceId, drill.Id, new AddAttachmentRequest("manual.pdf", Pdf), default);

        await space.Core.SpaceManager().DeleteSpaceAsync(space.Owen, space.SpaceId, default);

        await Assert.That(space.Core.Files.Files.Count).IsEqualTo(0);
    }

    private static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.SteelBlue);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
