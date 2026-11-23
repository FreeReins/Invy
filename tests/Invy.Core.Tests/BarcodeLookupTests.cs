using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Tests;

public class BarcodeLookupTests
{
    private static readonly Uri Photo = new("https://images.example/drill.jpg");

    [Test]
    public async Task Lookup_is_off_until_an_admin_turns_it_on()
    {
        var space = await ItemSpace.MakeAsync();
        space.Core.Products.Products["0012345678905"] = new ProductInfo("20V drill", "DeWalt", Photo, "UPCitemdb");

        var off = await TestData.Caught<RuleViolationException>(
            () => space.Items.LookUpBarcodeAsync(space.Owen, space.SpaceId, "0012345678905", default));
        await space.Core.AccountManager().UpdateSecurityAsync(new SecurityRequest(5, 15, false, BarcodeLookup: true), default);
        var found = await space.Items.LookUpBarcodeAsync(space.Owen, space.SpaceId, "0 012345-678905", default);

        await Assert.That(off?.Message).Contains("Barcode lookup is off");
        await Assert.That(found).IsEqualTo(new ProductResponse("0012345678905", "20V drill", "DeWalt", Photo.ToString(), "UPCitemdb"));
        await Assert.That(string.Join(", ", space.Core.Products.Asked)).IsEqualTo("0012345678905");
    }

    [Test]
    public async Task Text_that_is_not_a_barcode_and_unknown_barcodes_say_so()
    {
        var space = await ItemSpace.MakeAsync();
        await space.Core.AccountManager().UpdateSecurityAsync(new SecurityRequest(5, 15, false, BarcodeLookup: true), default);

        var notBarcode = await TestData.Caught<RuleViolationException>(() => space.Items.LookUpBarcodeAsync(space.Owen, space.SpaceId, "7K3Q", default));
        var unknown = await TestData.Caught<NotFoundException>(() => space.Items.LookUpBarcodeAsync(space.Owen, space.SpaceId, "12345670", default));

        await Assert.That(notBarcode?.Message).Contains("isn't a product barcode");
        await Assert.That(unknown?.Message).IsEqualTo("No product database knows barcode 12345670. Type its name instead.");
        await Assert.That(space.Core.Products.Asked.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_products_photo_is_downloaded_and_attached()
    {
        var space = await ItemSpace.MakeAsync();
        var drill = await space.ThingAsync("Drill");
        space.Core.Products.Web[Photo] = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

        var added = await space.Core.AttachmentManager().AttachFromWebAsync(space.Owen, space.SpaceId, drill.Id, new AttachFromWebRequest(Photo.ToString(), null), default);

        await Assert.That(added.Name).IsEqualTo("drill.jpg");
        await Assert.That(added.ContentType).IsEqualTo("image/jpeg");
        await Assert.That(added.Kind).IsEqualTo(AttachmentKind.Photo);
    }
}
