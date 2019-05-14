﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfXenon.Standard
{
    public abstract class Renderer : RenderObject
    {
        public Renderer()
            : base(null)
        {
            GraphicsState = new RenderGraphicsState(this, true);
            Operands = new Stack<PdfObject>();
        }

        public override void Visit(IRenderObjectVisitor visitor)
        {
            visitor.Visit(this);
        }

        public IRendererResolver Resolver { get; set; }
        public RenderGraphicsState GraphicsState { get; private set; }
        public Stack<PdfObject> Operands { get; private set; }

        public abstract void Initialize(PdfRectangle mediaBox, PdfRectangle cropBox);
        public abstract void SubPathStart(RenderPoint pt);
        public abstract void SubPathLineTo(RenderPoint pt);
        public abstract void SubPathBezier(RenderPoint pt2, RenderPoint pt3, RenderPoint pt4);
        public abstract void SubPathClose();
        public abstract void PathRectangle(RenderPoint pt, float width, float height);
        public abstract void PathStroke();
        public abstract void PathFill(bool evenOdd);
        public abstract void PathShading(RenderPatternShading shading);
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
                        PushGraphicsState();
                        break;
                    case "Q": // Restore graphics state
                        PopGraphicsState();
                        break;
                    case "cm":
                        {
                            float f = OperandAsNumber();
                            float e = OperandAsNumber();
                            float d = OperandAsNumber();
                            float c = OperandAsNumber();
                            float b = OperandAsNumber();
                            float a = OperandAsNumber();
                            GraphicsState.CTM = new RenderMatrix(a, b, c, d, e, f);
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
                            SubPathStart(new RenderPoint(OperandAsNumber(), y));
                        }
                        break;
                    case "l": // Append straight line segment to subpath
                        {
                            float y = OperandAsNumber();
                            SubPathLineTo(new RenderPoint(OperandAsNumber(), y));
                        }
                        break;
                    case "c": // Append cubic Bezier curve to subpath
                        {
                            float y3 = OperandAsNumber();
                            RenderPoint pt3 = new RenderPoint(OperandAsNumber(), y3);
                            float y2 = OperandAsNumber();
                            RenderPoint pt2 = new RenderPoint(OperandAsNumber(), y2);
                            float y1 = OperandAsNumber();
                            RenderPoint pt1 = new RenderPoint(OperandAsNumber(), y1);
                            SubPathBezier(pt1, pt2, pt3);
                        }
                        break;
                    case "v": // Append cubic Bezier curve to subpath (first control point is same as initial point)
                        {
                            float y3 = OperandAsNumber();
                            RenderPoint pt3 = new RenderPoint(OperandAsNumber(), y3);
                            float y2 = OperandAsNumber();
                            RenderPoint pt2 = new RenderPoint(OperandAsNumber(), y2);
                            SubPathBezier(pt2, pt2, pt3);
                        }
                        break;
                    case "y": // Append cubic Bezier curve to subpath (second control point is same as final point)
                        {
                            float y3 = OperandAsNumber();
                            RenderPoint pt3 = new RenderPoint(OperandAsNumber(), y3);
                            float y1 = OperandAsNumber();
                            RenderPoint pt1 = new RenderPoint(OperandAsNumber(), y1);
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
                            RenderPoint pt = new RenderPoint(OperandAsNumber(), y);
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
                    case "sh":
                        PathShading(RenderPatternShading.ParseShading(this, Resolver.GetShadingObject(OperandAsString()), null, null));
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
                        GraphicsState.ColorSpaceStroking = RenderColorSpace.FromName(this, OperandAsString());
                        break;
                    case "cs": // Set the current Color Space for non-stroking
                        GraphicsState.ColorSpaceNonStroking = RenderColorSpace.FromName(this, OperandAsString());
                        break;
                    case "SC": // Set the color for stroking
                        GraphicsState.ColorSpaceStroking.ParseParameters();
                        break;
                    case "sc": // Set the color for non-stroking
                        GraphicsState.ColorSpaceNonStroking.ParseParameters();
                        break;
                    case "SCN": // Set the color for stroking
                        GraphicsState.ColorSpaceStroking.ParseParameters();
                        break;
                    case "scn": // Set the color for non-stroking
                        GraphicsState.ColorSpaceNonStroking.ParseParameters();
                        break;
                    case "G": // Set a gray and the color space to DeviceGray for stroking
                        GraphicsState.ColorSpaceStroking = RenderColorSpace.FromName(this, "DeviceGray");
                        GraphicsState.ColorSpaceStroking.ParseParameters();
                        break;
                    case "g": // Set a gray and the color space to DeviceGray for non-stroking
                        GraphicsState.ColorSpaceNonStroking = RenderColorSpace.FromName(this, "DeviceGray");
                        GraphicsState.ColorSpaceNonStroking.ParseParameters();
                        break;
                    case "RG": // Set a color and the color space to DeviceRGB for stroking
                        GraphicsState.ColorSpaceStroking = RenderColorSpace.FromName(this, "DeviceRGB");
                        GraphicsState.ColorSpaceStroking.ParseParameters();
                        break;
                    case "rg": // Set a color and the color space to DeviceRGB for non-stroking
                        GraphicsState.ColorSpaceNonStroking = RenderColorSpace.FromName(this, "DeviceRGB");
                        GraphicsState.ColorSpaceNonStroking.ParseParameters();
                        break;
                    case "K": // Set a color and the color space to DeviceCMYK for stroking
                        GraphicsState.ColorSpaceStroking = RenderColorSpace.FromName(this, "DeviceCMYK");
                        GraphicsState.ColorSpaceStroking.ParseParameters();
                        break;
                    case "k": // Set a color and the color space to DeviceCMYK for non-stroking
                        GraphicsState.ColorSpaceNonStroking = RenderColorSpace.FromName(this, "DeviceCMYK");
                        GraphicsState.ColorSpaceNonStroking.ParseParameters();
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

        public void PushGraphicsState()
        {
            GraphicsState = new RenderGraphicsState(GraphicsState);
        }

        public void PopGraphicsState()
        {
            GraphicsState = GraphicsState.ParentGraphicsState;
        }

        public void ProcessExtGState(PdfDictionary dictionary)
        {
            foreach (var entry in dictionary)
            {
                switch (entry.Key)
                {
                    case "LW": // Set Line Width
                        GraphicsState.LineWidth = entry.Value.AsNumber();
                        break;
                    case "LC": // Set Line Cap Style
                        GraphicsState.LineCapStyle = entry.Value.AsInteger();
                        break;
                    case "LJ": // Set Line Join Style
                        GraphicsState.LineJoinStyle = entry.Value.AsInteger();
                        break;
                    case "ML": // Set Miter Length
                        GraphicsState.MiterLength = entry.Value.AsNumber();
                        break;
                    case "D": // Set Dash
                        {
                            List<PdfObject> array = entry.Value.AsArray();
                            GraphicsState.DashArray = array[0].AsNumberArray();
                            GraphicsState.DashPhase = array[1].AsInteger();
                        }
                        break;
                    case "RI": // Set Rendering Intent
                        GraphicsState.RenderingIntent = entry.Value.AsString();
                        break;
                    case "OP": // Set Over Print (available in all versions)
                        GraphicsState.OverPrint10 = entry.Value.AsBoolean();
                        break;
                    case "op": // Set Over Print (available in version 1.3 onwards)
                        GraphicsState.OverPrint13 = entry.Value.AsBoolean();
                        break;
                    case "OPM": // Set Overprint Mode
                        GraphicsState.OverPrintMode = entry.Value.AsInteger();
                        break;
                    case "BM": // Set Blend Mode
                        GraphicsState.BlendMode = entry.Value;
                        break;
                    case "CA": // Set Constant Alpha for Stroking
                        GraphicsState.ConstantAlphaStroking = entry.Value.AsNumber();
                        break;
                    case "ca": // Set Constant Alpha for Non-Stroking
                        GraphicsState.ConstantAlphaNonStroking = entry.Value.AsNumber();
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
                        GraphicsState.FlatnessTolerance = entry.Value.AsNumber();
                        break;
                    case "SM": // Set Smoothness tolerance
                        GraphicsState.SmoothnessTolerance = entry.Value.AsNumber();
                        break;
                    case "SA": // Set Stroke adjustment flag
                        GraphicsState.StrokeAdjustment = entry.Value.AsBoolean();
                        break;
                    case "SMask": // Set Soft Mask
                        GraphicsState.SoftMask = entry.Value;
                        break;
                    case "AIS": // Set Alpha Source Mask
                        GraphicsState.AlphaSourceMask = entry.Value.AsBoolean();
                        break;
                    case "TK": // Set Text Knockout flag
                        GraphicsState.TextKnockout = entry.Value.AsBoolean();
                        break;
                    default:
                        // Ignore anything we do not recognize
                        break;
                }
            }
        }

        public bool OperandAsBoolean()
        {
            return Operands.Pop().AsBoolean();
        }

        public string OperandAsString()
        {
            return Operands.Pop().AsString();
        }

        public int OperandAsInteger()
        {
            return Operands.Pop().AsInteger();
        }

        public float OperandAsNumber()
        {
            return Operands.Pop().AsNumber();
        }

        public List<PdfObject> OperandAsArray()
        {
            return Operands.Pop().AsArray();
        }

        public float[] OperandAsNumberArray()
        {
            return Operands.Pop().AsNumberArray();
        }
    }
}
