﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Line<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly Dictionary<(string, LineType), Path> paths = new Dictionary<(string, LineType), Path>();

        public Line(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            IAestheticMapping<T, string> color = null,
            IAestheticMapping<T, LineType> lineType = null,
            Func<T, string[]> tooltip = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Tooltip = tooltip
            };

            Aesthetics = new _Aesthetics
            {
                Color = color,
                LineType = lineType
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, string[]> Tooltip { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Aesthetics
        {
            public IAestheticMapping<T, string> Color { get; set; }

            public IAestheticMapping<T, LineType> LineType { get; set; }
        }

        public _Aesthetics Aesthetics { get; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> Y { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

        public Elements.Line Aesthetic { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            if (Selectors.X == null)
            {
                Positions.X = XMapping(panel.Data.Selectors.X, panel.X);
            }
            else
            {
                Positions.X = XMapping(Selectors.X, panel.X);
            }

            if (Selectors.Y == null)
            {
                Positions.Y = YMapping(panel.Data.Selectors.Y, panel.Y);
            }
            else
            {
                Positions.Y = YMapping(Selectors.Y, panel.Y);
            }

            if (OnMouseOver == null && OnMouseOut == null && Selectors.Tooltip != null)
            {
                OnMouseOver = (item, _) =>
                {
                    panel.Component.Tooltip.Show(
                        Positions.X.Map(item),
                        Positions.Y.Map(item),
                        Selectors.Tooltip(item),
                        Aesthetics.Color?.Map(item) ?? Aesthetic.Fill);

                    return Task.CompletedTask;
                };

                OnMouseOut = (_, __) =>
                {
                    panel.Component.Tooltip.Hide();

                    return Task.CompletedTask;
                };
            }

            if (!inherit)
            {
                return;
            }

            Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
            Aesthetics.LineType ??= panel.Data.Aesthetics.LineType as IAestheticMapping<T, LineType>;
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Y.Train(item);

            Aesthetics.Color?.Train(item);
            Aesthetics.LineType?.Train(item);
        }

        public override void Legend()
        {
            Legend(Aesthetics.Color, value => new Elements.HLine
            {
                Width = Aesthetic.Width,
                Fill = value,
                Alpha = Aesthetic.Alpha,
                LineType = Aesthetic.LineType
            });

            Legend(Aesthetics.LineType, value => new Elements.HLine
            {
                Width = Aesthetic.Width,
                Fill = Aesthetic.Fill,
                Alpha = Aesthetic.Alpha,
                LineType = value
            });
        }

        protected override void Shape(T item, bool flip)
        {
            var color = Aesthetic.Fill;

            if (Aesthetics.Color != null)
            {
                color = Aesthetics.Color.Map(item);
                if (string.IsNullOrEmpty(color))
                {
                    return;
                }
            }

            var lineType = Aesthetic.LineType;
            if (Aesthetics.LineType != null)
            {
                lineType = Aesthetics.LineType.Map(item);
            }

            if (!paths.TryGetValue((color, lineType), out var path))
            {
                path = new Path
                {
                    Aesthetic = new Elements.Line
                    {
                        Width = Aesthetic.Width,
                        Fill = color,
                        Alpha = Aesthetic.Alpha,
                        LineType = lineType
                    }
                };

                Layer.Add(path);

                paths[(color, lineType)] = path;
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            path.Points.Add((x, y));

            if (OnClick != null || OnMouseOver != null || OnMouseOut != null)
            {
                var circle = new Circle
                {
                    X = x,
                    Y = y,
                    Aesthetic = new Elements.Circle
                    {
                        Radius = 3.0 * Aesthetic.Width,
                        Alpha = 0,
                        Fill = "transparent"
                    }
                };

                if (OnClick != null)
                {
                    circle.OnClick = e => OnClick(item, e);
                }

                if (OnMouseOver != null)
                {
                    circle.OnMouseOver = e => OnMouseOver(item, e);
                }

                if (OnMouseOut != null)
                {
                    circle.OnMouseOut = e => OnMouseOut(item, e);
                }

                Layer.Add(circle);
            }

            Positions.X.Position.Shape(x, x);
            Positions.Y.Position.Shape(y, y);
        }

        public override void Clear()
        {
            base.Clear();

            paths.Clear();
        }
    }
}
