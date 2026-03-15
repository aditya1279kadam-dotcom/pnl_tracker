using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using FinanceOS.Backend.Models;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace FinanceOS.Backend.Services
{
    public interface IExportService
    {
        byte[] GenerateExcel(IEnumerable<object> data, string sheetName);
        byte[] GeneratePowerPoint(FinanceCalculationResult financeResults);
    }

    public class ExportService : IExportService
    {
        public byte[] GenerateExcel(IEnumerable<object> data, string sheetName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).InsertTable(data);
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GeneratePowerPoint(FinanceCalculationResult financeResults)
        {
            using var stream = new MemoryStream();
            using (var presentationDocument = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
            {
                var presentationPart = presentationDocument.AddPresentationPart();
                presentationPart.Presentation = new Presentation();
                
                var slideMasterIdList = new SlideMasterIdList(new SlideMasterId { Id = 2147483648, RelationshipId = "rId1" });
                var slideIdList = new SlideIdList();
                var slideSize = new SlideSize { Cx = 9144000, Cy = 6858000, Type = SlideSizeValues.Screen4x3 };
                var notesSize = new NotesSize { Cx = 6858000, Cy = 9144000 };
                
                presentationPart.Presentation.Append(slideMasterIdList, slideIdList, slideSize, notesSize);
                
                // Add first slide (Title)
                AddSlide(presentationPart, slideIdList, 256, "Finance OS Report", $"Total Revenue: {financeResults.Summary.TotalRevenue:C}");
                
                // Add second slide (KPIs)
                AddSlide(presentationPart, slideIdList, 257, "Key Performance Indicators", 
                    $"Total Profit: {financeResults.Summary.TotalProfit:C}\nAverage Margin: {financeResults.Summary.AvgMargin:P2}");
                
                presentationPart.Presentation.Save();
            }
            return stream.ToArray();
        }

        private void AddSlide(PresentationPart presentationPart, SlideIdList slideIdList, uint id, string title, string bodyText)
        {
            var slidePart = presentationPart.AddNewPart<SlidePart>($"rId{id}");
            var slide = new Slide(new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new A.TransformGroup()),
                new P.Shape(
                    new P.NonVisualShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 2, Name = "Title" },
                        new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                        new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = PlaceholderValues.Title })),
                    new P.ShapeProperties(),
                    new P.TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.Run(new A.Text(title))))),
                new P.Shape(
                    new P.NonVisualShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 3, Name = "Body" },
                        new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                        new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape { Type = PlaceholderValues.Body })),
                    new P.ShapeProperties(),
                    new P.TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.Run(new A.Text(bodyText)))))
            )));
            slidePart.Slide = slide;

            var slideId = new SlideId { Id = id, RelationshipId = $"rId{id}" };
            slideIdList.Append(slideId);
        }
    }
}
