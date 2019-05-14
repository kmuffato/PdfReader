﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PdfXenon.Standard
{
    public class RenderDebugBuilder : IRenderObjectVisitor
    {
        private RenderObject _obj;
        private StringBuilder _sb = new StringBuilder();
        private Stack<int> _indents = new Stack<int>();
        private int _index;

        public RenderDebugBuilder()
            : this(null)
        {
        }

        public RenderDebugBuilder(RenderObject obj)
        {
            _obj = obj;
        }

        public override string ToString()
        {
            if (_obj != null)
            {
                _index = 0;
                _indents = new Stack<int>();
                _indents.Push(_index);
                _sb = new StringBuilder();
                _obj.Visit(this);
                return _sb.ToString();
            }

            return string.Empty;
        }

        public void Visit(Renderer renderer)
        {
            PushNextLevel();
            Append("Renderer");

            VisitNotNull(renderer.GraphicsState);

            PopLevel();
            CurrentLevelNewLine();
        }

        public void Visit(RenderGraphicsState state)
        {
            PushNextLevel();
            Append("RenderGraphicsState");
            CurrentLevelNewLine();

            Append($"Depth {state.Depth}");
            AppendObject("CTM", state.LocalCTM);
            AppendObject("LineWidth", state.LocalLineWidth);
            AppendObject("LineCapStyle", state.LocalLineCapStyle);
            AppendObject("LineJoinStyle", state.LocalLineJoinStyle);
            AppendObject("MiterLength", state.LocalMiterLength);
            AppendObject("DashArray", state.LocalDashArray);
            AppendObject("DashPhase", state.LocalDashPhase);
            AppendObject("RenderingIntent", state.LocalRenderingIntent);
            AppendObject("Flatness", state.LocalFlatness);
            AppendObject("OverPrint10", state.LocalOverPrint10);
            AppendObject("OverPrint13", state.LocalOverPrint13);
            AppendObject("OverPrintMode", state.LocalOverPrintMode);
            AppendObject("ConstantAlphaStroking", state.LocalConstantAlphaStroking);
            AppendObject("ConstantAlphaNonStroking", state.LocalConstantAlphaNonStroking);
            AppendObject("TextKnockout", state.LocalTextKnockout);
            VisitNotNull(state.LocalColorSpaceStroking, "ColorSpaceStroking");
            VisitNotNull(state.LocalColorSpaceNonStroking, "ColorSpaceNonStroking");
            AppendObject("BlendMode", state.LocalBlendMode);
            AppendObject("Font", state.LocalFont);
            AppendObject("BlackGeneration", state.LocalBlackGeneration);
            AppendObject("BlackGeneration2", state.LocalBlackGeneration2);
            AppendObject("UndercolorRemoval", state.LocalUndercolorRemoval);
            AppendObject("UndercolorRemoval2", state.LocalUndercolorRemoval2);
            AppendObject("Transfer", state.LocalTransfer);
            AppendObject("Transfer2", state.LocalTransfer2);
            AppendObject("Halftone", state.LocalHalftone);
            AppendObject("SoftMask", state.LocalSoftMask);
            AppendObject("Clipping", state.LocalClipping);

            PopLevel();
            CurrentLevelNewLine();

            if (state.ParentGraphicsState != null)
                state.ParentGraphicsState.Visit(this);
        }

        public void Visit(RenderColorSpacePattern pattern)
        {
            Append("RenderColorSpacePattern ");
            pattern.GetPattern().Visit(this);
        }

        public void Visit(RenderPatternShadingAxial axial)
        {
            PushNextLevel();
            Append("RenderPatternShadingAxial");

            AppendObject($"ShadingType", axial.ShadingType);
            AppendObject($"ColorSpace", axial.ColorSpace);
            AppendObject($"Background", axial.Background);
            AppendObject($"BBox", axial.BBox);
            AppendObject($"AntiAlias", axial.AntiAlias);
            AppendObject($"Coords", axial.Coords);
            AppendObject($"Domain", axial.Domain);
            AppendObject($"Function", axial.Function);
            AppendObject($"Extend", axial.Extend);

            PopLevel();
            CurrentLevelNewLine();
        }

        public void Visit(RenderObject obj)
        {
            Append(obj);
        }

        private void VisitNotNull(RenderObject obj, bool newLine = true)
        {
            if (obj != null)
            {
                if (newLine)
                    CurrentLevelNewLine();

                obj.Visit(this);
            }
        }

        private void VisitNotNull(RenderObject obj, string name, bool newLine = true)
        {
            if (obj != null)
            {
                if (newLine)
                    CurrentLevelNewLine();

                Append($"{name} ");
                obj.Visit(this);
            }
        }

        private void PushLevel()
        {
            _indents.Push(_index);
        }

        private void PushNextLevel()
        {
            _indents.Push(_index + 2);
        }

        private void PopLevel()
        {
            _index = _indents.Pop();
        }

        private void Append(object obj)
        {
            string str = obj.ToString();
            _sb.Append(str);
            _index += str.Length;
        }

        private void AppendObject(string name, object obj)
        {
            if (obj != null)
            {
                CurrentLevelNewLine();
                Append($"{name} {obj.ToString()}");
            }
        }

        private void CurrentLevelNewLine()
        {
            int indent = _indents.Peek();
            _index = indent;
            _sb.Append($"\n{new string(' ', _index)}");
        }
    }
}