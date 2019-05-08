﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfXenon.Standard
{
    public abstract class PdfRenderer
    {
        public PdfRenderer()
        {
            GraphicsState = new PdfGraphicsState();
            Operands = new Stack<PdfObject>();
        }

        public IPdfRendererResolver Resolver { get; set; }
        public PdfGraphicsState GraphicsState { get; private set; }
        public Stack<PdfObject> Operands { get; private set; }

        public abstract void Initialize(PdfRectangle mediaBox, PdfRectangle cropBox);
        public abstract void SubPathStart(PdfPoint pt);
        public abstract void SubPathLineTo(PdfPoint pt);
        public abstract void SubPathBezier(PdfPoint pt2, PdfPoint pt3, PdfPoint pt4);
        public abstract void SubPathClose();
        public abstract void PathRectangle(PdfPoint pt, float width, float height);
        public abstract void PathStroke();
        public abstract void PathFill(bool evenOdd);
        public abstract void PathClip(bool evenOdd);
        public abstract void PathEnd();
        public abstract void Finshed();

        public void Render(PdfObject obj)
        {
            if (obj is PdfIdentifier identifer)
            {
                switch (identifer.Value)
                {
                    case "q": // Save graphics state
                        GraphicsState = new PdfGraphicsState(GraphicsState);
                        break;
                    case "Q": // Restore graphics state
                        GraphicsState = GraphicsState.ParentGraphicsState;
                        break;
                    case "cm":
                        {
                            float f = OperandAsNumber();
                            float e = OperandAsNumber();
                            float d = OperandAsNumber();
                            float c = OperandAsNumber();
                            float b = OperandAsNumber();
                            float a = OperandAsNumber();
                            GraphicsState.CTM = new PdfMatrix(a, b, c, d, e, f);
                        }
                        break;
                    case "w": // Set Line Width
                        GraphicsState.LineWidth = OperandAsNumber();
                        break;
                    case "j": // Set Line Join Style
                        GraphicsState.LineJoinStyle = OperandAsInteger();
                        break;
                    case "J": // Set Line Cap Style
                        GraphicsState.LineCapStyle = OperandAsInteger();
                        break;
                    case "M": // Set Miter Length
                        GraphicsState.MiterLength = OperandAsNumber();
                        break;
                    case "d": // Set Dash
                        GraphicsState.DashPhase = OperandAsInteger();
                        GraphicsState.DashArray = OperandAsNumberArray();
                        break;
                    case "ri": // Set Rendering Intent
                        GraphicsState.RenderingIntent = OperandAsString();
                        break;
                    case "i": // Set Flatness
                        GraphicsState.Flatness = OperandAsNumber();
                        break;
                    case "gs": // Set graphics state from dictionary
                        ProcessExtGState(Resolver.GetGraphicsStateDictionary(OperandAsString()));
                        break;
                    case "m": // Start a new subpath and set currrent point
                        {
                            float y = OperandAsNumber();
                            SubPathStart(new PdfPoint(OperandAsNumber(), y));
                        }
                        break;
                    case "l": // Append straight line segment to subpath
                        {
                            float y = OperandAsNumber(Operands.Pop());
                            SubPathLineTo(new PdfPoint(OperandAsNumber(), y));
                        }
                        break;
                    case "c": // Append cubic Bezier curve to subpath
                        {
                            float y3 = OperandAsNumber();
                            PdfPoint pt3 = new PdfPoint(OperandAsNumber(), y3);
                            float y2 = OperandAsNumber();
                            PdfPoint pt2 = new PdfPoint(OperandAsNumber(), y2);
                            float y1 = OperandAsNumber();
                            PdfPoint pt1 = new PdfPoint(OperandAsNumber(), y1);
                            SubPathBezier(pt1, pt2, pt3);
                        }
                        break;
                    case "v": // Append cubic Bezier curve to subpath (first control point is same as initial point)
                        {
                            float y3 = OperandAsNumber();
                            PdfPoint pt3 = new PdfPoint(OperandAsNumber(), y3);
                            float y2 = OperandAsNumber();
                            PdfPoint pt2 = new PdfPoint(OperandAsNumber(), y2);
                            SubPathBezier(pt2, pt2, pt3);
                        }
                        break;
                    case "y": // Append cubic Bezier curve to subpath (second control point is same as final point)
                        {
                            float y3 = OperandAsNumber();
                            PdfPoint pt3 = new PdfPoint(OperandAsNumber(), y3);
                            float y1 = OperandAsNumber();
                            PdfPoint pt1 = new PdfPoint(OperandAsNumber(), y1);
                            SubPathBezier(pt1, pt1, pt3);
                        }
                        break;
                    case "h": // Close subpath
                        SubPathClose();
                        break;
                    case "re": // Append rectange as complete subpath
                        {
                            float h = OperandAsNumber();
                            float w = OperandAsNumber();
                            float y = OperandAsNumber();
                            PdfPoint pt = new PdfPoint(OperandAsNumber(), y);
                            PathRectangle(pt, w, h);
                        }
                        break;
                    case "S": // Stroke the path
                        PathStroke();
                        PathEnd();
                        break;
                    case "s": // Close and Stroke the path
                        SubPathClose();
                        PathStroke();
                        PathEnd();
                        break;
                    case "f": // Fill the path
                    case "F": // Fill the path (used in older versions, does same as 'f')
                        PathFill(false);
                        PathEnd();
                        break;
                    case "f*": // Fill the path using the even-odd rule
                        PathFill(true);
                        PathEnd();
                        break;
                    case "B": // Fill and Stroke the path
                        PathFill(false);
                        PathStroke();
                        PathEnd();
                        break;
                    case "B*": // Fill and Stroke the path using the even-odd rule
                        PathFill(true);
                        PathStroke();
                        PathEnd();
                        break;
                    case "b": // Close, Fill and Stroke the path
                        SubPathClose();
                        PathFill(false);
                        PathStroke();
                        PathEnd();
                        break;
                    case "b*": // Close, Fill and Stroke the path using the even-odd rule
                        SubPathClose();
                        PathFill(true);
                        PathStroke();
                        PathEnd();
                        break;
                    case "n": // End the path without filling or stroking it
                        PathEnd();
                        break;
                    case "W": // Update clipping path when current path is ended
                        PathClip(false);
                        break;
                    case "W*": // Update clipping path when current path is ended using the even-odd rule
                        PathClip(true);
                        break;
                    case "CS": // Set the current Color Space for stroking
                        GraphicsState.ColorSpaceStroking = PdfColorSpace.FromName(this, OperandAsString());
                        break;
                    case "cs": // Set the current Color Space for non-stroking
                        GraphicsState.ColorSpaceNonStroking = PdfColorSpace.FromName(this, OperandAsString());
                        break;
                    case "SC": // Set the color for stroking
                        GraphicsState.ColorSpaceStroking.ParseColor();
                        break;
                    case "sc": // Set the color for non-stroking
                        GraphicsState.ColorSpaceNonStroking.ParseColor();
                        break;
                    case "SCN": // Set the color for stroking
                        GraphicsState.ColorSpaceStroking.ParseColor();
                        break;
                    case "scn": // Set the color for non-stroking
                        GraphicsState.ColorSpaceNonStroking.ParseColor();
                        break;
                    case "G": // Set a gray and the color space to DeviceGray for stroking
                        GraphicsState.ColorSpaceStroking = PdfColorSpace.FromName(this, "DeviceGray");
                        GraphicsState.ColorSpaceStroking.ParseColor();
                        break;
                    case "g": // Set a gray and the color space to DeviceGray for non-stroking
                        GraphicsState.ColorSpaceNonStroking = PdfColorSpace.FromName(this, "DeviceGray");
                        GraphicsState.ColorSpaceNonStroking.ParseColor();
                        break;
                    case "RG": // Set a color and the color space to DeviceRGB for stroking
                        GraphicsState.ColorSpaceStroking = PdfColorSpace.FromName(this, "DeviceRGB");
                        GraphicsState.ColorSpaceStroking.ParseColor();
                        break;
                    case "rg": // Set a color and the color space to DeviceRGB for non-stroking
                        GraphicsState.ColorSpaceNonStroking = PdfColorSpace.FromName(this, "DeviceRGB");
                        GraphicsState.ColorSpaceNonStroking.ParseColor();
                        break;
                    case "K": // Set a color and the color space to DeviceCMYK for stroking
                        GraphicsState.ColorSpaceStroking = PdfColorSpace.FromName(this, "DeviceCMYK");
                        GraphicsState.ColorSpaceStroking.ParseColor();
                        break;
                    case "k": // Set a color and the color space to DeviceCMYK for non-stroking
                        GraphicsState.ColorSpaceNonStroking = PdfColorSpace.FromName(this, "DeviceCMYK");
                        GraphicsState.ColorSpaceNonStroking.ParseColor();
                        break;
                    default:
                        // Ignore anything we do not recognize
                        break;
                }
            }
            else if (obj is PdfName name)
            {
                switch (name.Value)
                {
                    default:
                        // If not an identifier that happens to match a Name, then must be an operand
                        Operands.Push(name);
                        break;
                }
            }

            // Any other type must be an operand
            Operands.Push(obj);
        }

        public void ProcessExtGState(PdfDictionary dictionary)
        {
            foreach (var entry in dictionary)
            {
                switch (entry.Key)
                {
                    case "LW": // Set Line Width
                        GraphicsState.LineWidth = OperandAsNumber(entry.Value);
                        break;
                    case "LC": // Set Line Cap Style
                        GraphicsState.LineCapStyle = OperandAsInteger(entry.Value);
                        break;
                    case "LJ": // Set Line Join Style
                        GraphicsState.LineJoinStyle = OperandAsInteger(entry.Value);
                        break;
                    case "ML": // Set Miter Length
                        GraphicsState.MiterLength = OperandAsNumber(entry.Value);
                        break;
                    case "D": // Set Dash
                        {
                            List<PdfObject> array = OperandAsArray(entry.Value);
                            GraphicsState.DashArray = OperandAsNumberArray(array[0]);
                            GraphicsState.DashPhase = OperandAsInteger(array[1]);
                        }
                        break;
                    case "RI": // Set Rendering Intent
                        GraphicsState.RenderingIntent = OperandAsString(entry.Value);
                        break;
                    case "OP": // Set Over Print (available in all versions)
                        GraphicsState.OverPrint10 = OperandAsBoolean(entry.Value);
                        break;
                    case "op": // Set Over Print (available in version 1.3 onwards)
                        GraphicsState.OverPrint13 = OperandAsBoolean(entry.Value);
                        break;
                    case "OPM": // Set Overprint Mode
                        GraphicsState.OverPrintMode = OperandAsInteger(entry.Value);
                        break;
                    case "BM": // Set Blend Mode
                        GraphicsState.BlendMode = entry.Value;
                        break;
                    case "CA": // Set Constant Alpha for Stroking
                        GraphicsState.ConstantAlphaStroking = OperandAsNumber(entry.Value);
                        break;
                    case "ca": // Set Constant Alpha for Non-Stroking
                        GraphicsState.ConstantAlphaNonStroking = OperandAsNumber(entry.Value);
                        break;
                    case "Font": // Font parameters
                        GraphicsState.Font = entry.Value;
                        break;
                    case "BG": // Set Black generation function
                        GraphicsState.BlackGeneration = entry.Value;
                        break;
                    case "BG2": // Set Black generation function or name
                        GraphicsState.BlackGeneration2 = entry.Value;
                        break;
                    case "UCR": // Set Undercolor removal function
                        GraphicsState.UndercolorRemoval = entry.Value;
                        break;
                    case "UCR2": // Set Undercolor removal function or name
                        GraphicsState.UndercolorRemoval2 = entry.Value;
                        break;
                    case "TR": // Set Transfer function
                        GraphicsState.Transfer = entry.Value;
                        break;
                    case "TR2": // Set Transfer function or name
                        GraphicsState.Transfer2 = entry.Value;
                        break;
                    case "HT": // Set Halftone
                        GraphicsState.Halftone = entry.Value;
                        break;
                    case "FL": // Set Flatness tolerance
                        GraphicsState.FlatnessTolerance = OperandAsNumber(entry.Value);
                        break;
                    case "SM": // Set Smoothness tolerance
                        GraphicsState.SmoothnessTolerance = OperandAsNumber(entry.Value);
                        break;
                    case "SA": // Set Stroke adjustment flag
                        GraphicsState.StrokeAdjustment = OperandAsBoolean(entry.Value);
                        break;
                    case "SMask": // Set Soft Mask
                        GraphicsState.SoftMask = entry.Value;
                        break;
                    case "AIS": // Set Alpha Source Mask
                        GraphicsState.AlphaSourceMask = OperandAsBoolean(entry.Value);
                        break;
                    case "TK": // Set Text Knockout flag
                        GraphicsState.TextKnockout = OperandAsBoolean(entry.Value);
                        break;
                    default:
                        // Ignore anything we do not recognize
                        break;
                }
            }
        }

        public bool OperandAsBoolean()
        {
            return OperandAsBoolean(Operands.Pop());
        }

        public bool OperandAsBoolean(PdfObject obj)
        {
            if (obj is PdfBoolean boolean)
                return boolean.Value;

            throw new ApplicationException($"Unexpected object in content '{obj.GetType().Name}', expected a boolean.");
        }

        public string OperandAsString()
        {
            return OperandAsString(Operands.Pop());
        }

        public string OperandAsString(PdfObject obj)
        {
            if (obj is PdfName name)
                return name.Value;
            else if (obj is PdfString str)
                return str.Value;

            throw new ApplicationException($"Unexpected object in content '{obj.GetType().Name}', expected a string.");
        }

        public int OperandAsInteger()
        {
            return OperandAsInteger(Operands.Pop());
        }

        public int OperandAsInteger(PdfObject obj)
        {
            if (obj is PdfInteger integer)
                return integer.Value;

            throw new ApplicationException($"Unexpected object in content '{obj.GetType().Name}', expected an integer.");
        }

        public float OperandAsNumber()
        {
            return OperandAsNumber(Operands.Pop());
        }

        public float OperandAsNumber(PdfObject obj)
        {
            if (obj is PdfInteger integer)
                return integer.Value;
            else if (obj is PdfReal real)
                return real.Value;

            throw new ApplicationException($"Unexpected object in content '{obj.GetType().Name}', expected a number.");
        }

        public List<PdfObject> OperandAsArray()
        {
            return OperandAsArray(Operands.Pop());
        }

        public List<PdfObject> OperandAsArray(PdfObject obj)
        {
            if (obj is PdfArray array)
                return array.Objects;

            throw new ApplicationException($"Unexpected object in content '{obj.GetType().Name}', expected an integer array.");
        }

        public float[] OperandAsNumberArray()
        {
            return OperandAsNumberArray(Operands.Pop());
        }

        public float[] OperandAsNumberArray(PdfObject obj)
        {
            if (obj is PdfArray array)
            {
                List<float> numbers = new List<float>();
                foreach(PdfObject item in array.Objects)
                {
                    if (item is PdfInteger integer)
                        numbers.Add(integer.Value);
                    else if (item is PdfReal real)
                        numbers.Add(real.Value);
                    else
                        throw new ApplicationException($"Array contains object of type '{obj.GetType().Name}', expected only numbers.");

                }

                return numbers.ToArray();
            }

            throw new ApplicationException($"Unexpected object in content '{obj.GetType().Name}', expected an integer array.");
        }
    }
}
