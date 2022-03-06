using System;
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Wordle.Engine;

namespace Wordle.Bot.Functions;

static class GameBoardRenderingService
{

    public static void RenderGameStateAsImage(GameEngineState gameEngineState, Stream outputStream)
    {
        using (Image img = new Image<Rgba32>(170, 200))
        {

            var font = SystemFonts.CreateFont("Arial", 12, FontStyle.Regular);

            string[] words = gameEngineState.Attempts.Select(x => x.ToUpperInvariant()).ToArray();
            var matchState = gameEngineState.AttemptsMask.ToArray();
            /* 0 empty 
             * 1 not in word 
             * 2 in word wrong position 
             * 3 match */
            // int[][] matchState = new int[][]
            // {
            //         new int[] { 1, 3, 1, 1, 2 },
            //         new int[] { 1, 1, 3, 3, 2 },
            //         new int[] { 0, 0, 0, 0, 0 },
            //         new int[] { 0, 0, 0, 0, 0 },
            //         new int[] { 0, 0, 0, 0, 0 },
            //         new int[] { 0, 0, 0, 0, 0 },
            // };

            int pointSpacing = 4;
            int pointSize = 26;
            int leftMargin = 10;
            int topMargin = 10;
            PointF[] squarePolygonPoints = new PointF[] { new PointF(0, 0), new PointF(0, pointSize), new PointF(pointSize, pointSize), new PointF(pointSize, 0) };

            img.Mutate(ctx =>
            {
                ctx.Fill(Color.White); // white background image
                for (int attempIndex = 0; attempIndex < gameEngineState.MaxAttemptsCount; attempIndex++)
                {
                    for (int characterIndex = 0; characterIndex < gameEngineState.WordLength; characterIndex++)
                    {
                        var leftPosition = leftMargin + (pointSpacing + pointSize) * characterIndex;
                        var topPosition = topMargin + (pointSpacing + pointSize) * attempIndex;
                        var squarePoints = squarePolygonPoints.Select(p => p + new PointF(leftPosition, topPosition)).ToArray();
                        Color printColor = new Color(new Rgb24(120, 124, 126));
                        if (attempIndex < words.Length && characterIndex < words[attempIndex].Length)
                        if (attempIndex < matchState.Length && characterIndex < matchState[attempIndex].Length)
                        {
                            if (matchState[attempIndex][characterIndex] == PositionMatchMask.MatchInOtherPosition )
                                printColor = new Color(new Rgb24(201, 180, 88)); // yellow
                            else if (matchState[attempIndex][characterIndex] == PositionMatchMask.Matched)
                                printColor = new Color(new Rgb24(106, 170, 100)); // green

                            ctx.FillPolygon(printColor, squarePoints);
                        }
                        ctx.DrawPolygon(new Pen(printColor, 1), squarePoints);

                        if (attempIndex < words.Length && characterIndex < words[attempIndex].Length)
                            ctx.ApplyScalingWaterMarkSimple(font, words[attempIndex][characterIndex].ToString(), Color.White,
                                                        new PointF(leftPosition, topPosition), new Size(25), 2);
                    }
                }
            });

            img.Save(outputStream, new PngEncoder());
        }
    }

    private static IImageProcessingContext ApplyScalingWaterMarkSimple(
        this IImageProcessingContext processingContext,
        Font font,
        string text,
        Color color,
        PointF topLeft,
        Size boxSize,
        int padding = 3)
    {
        //Size imgSize = processingContext.GetCurrentSize();
        float targetWidth = boxSize.Width - (padding * 2);
        float targetHeight = boxSize.Height - (padding * 2);

        // measure the text size
        FontRectangle size = TextMeasurer.Measure(text, new TextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });

        //find out how much we need to scale the text to fill the space (up or down)
        float scalingFactor = Math.Min(targetWidth / size.Width, targetHeight / size.Height);

        //create a new font
        Font scaledFont = new Font(font, scalingFactor * font.Size);

        var center = new PointF(boxSize.Width / 2, 2 + boxSize.Height / 2) + topLeft;

        var textGraphicsOptions = new TextOptions(scaledFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = center,
            ApplyHinting = true,
            Dpi = 80,
            KerningMode = KerningMode.Normal,
        };

        return processingContext.DrawText(textGraphicsOptions, text, color);
    }
}