﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using VVVV.DX11.Internals.Effects.Pins;
using VVVV.DX11.Internals;
using VVVV.DX11.Lib.Rendering;
using FeralTic.DX11;

namespace VVVV.DX11.Lib.Effects.Registries
{
    public abstract class ShaderVariableDictionary<T> where T : IShaderVariable
    {
        private Dictionary<string, T> variablesDictionary = new Dictionary<string, T>();
        protected List<T> variablesList = new List<T>();

        public void UpdateEffect(Effect effect)
        {
            List<string> toremove = new List<string>();
            foreach (T shaderpin in this.variablesDictionary.Values)
            {
                bool needdelete = true;
                for (int i = 0; i < effect.Description.GlobalVariableCount; i++)
                {
                    EffectVariable var = effect.GetVariableByIndex(i);

                    if (Match(shaderpin,var))
                    {
                        //Found variable, no need to delete, but call update on variable
                        shaderpin.Update(var);
                        needdelete = false;
                    }
                }
                if (needdelete)
                {
                    toremove.Add(shaderpin.Name);
                }
            }

            foreach (string s in toremove)
            {
                T var = this.variablesDictionary[s];     
                var.Dispose();
                this.variablesDictionary.Remove(s);
                this.variablesList.Remove(var);
            }
        }

        public bool Contains(string name)
        {
            return this.variablesDictionary.ContainsKey(name);
        }

        public void Add(string name, T data)
        {
            this.variablesDictionary[name] = data;
            this.variablesList.Add(data);
        }

        protected abstract bool Match(T element, EffectVariable var);


    }

    public class ShaderPinDictionary : ShaderVariableDictionary<IShaderPin>
    {
        private List<IShaderPin> spreadedpins = new List<IShaderPin>();

        protected override bool Match(IShaderPin element, EffectVariable var)
        {
            return var.Match(element);
        }

        public int SpreadMax
        {
            get
            {
                if (this.variablesList.Count == 0) { return 1; }

                int max = 0;
                for (int i = 0; i < this.variablesList.Count; i++)
                {
                    IShaderPin pin = this.variablesList[i];
                    if (pin.SliceCount == 0) { return 0; }
                    max = Math.Max(pin.SliceCount, max);
                }
                return max;
            }
        }

        public void Preprocess(DX11ShaderInstance instance)
        {
            this.spreadedpins.Clear();
            for (int i = 0; i < this.variablesList.Count; i++)
            {
                IShaderPin sp = this.variablesList[i];
                //sp.RenderContext = context;

                if (sp.Constant) { sp.SetVariable(instance, 0); }
                else { spreadedpins.Add(sp); }
            }

            foreach (IShaderPin sp in spreadedpins)
            {
                sp.SetVariable(instance, 0);
            }
        }

        public void ApplySlice(DX11ShaderInstance instance, int slice)
        {
            for (int i = 0; i < this.spreadedpins.Count; i++)
            {
                spreadedpins[i].SetVariable(instance, slice);
            }
        }
    }

    public class RenderVariableDictionary : ShaderVariableDictionary<IRenderVariable>
    {
        protected override bool Match(IRenderVariable element, EffectVariable var)
        {
            return !var.NeedDestroy(element);
        }

        public void Apply(DX11ShaderInstance instance, DX11RenderSettings settings)
        {
            for (int i = 0; i < this.variablesList.Count; i++)
            {
                this.variablesList[i].Apply(instance, settings);
            }
        }
    }

    public class WorldRenderVariableDictionary : ShaderVariableDictionary<IWorldRenderVariable>
    {
        protected override bool Match(IWorldRenderVariable element, EffectVariable var)
        {
            return !var.NeedDestroy(element);
        }

        public void Apply(DX11ShaderInstance instance, DX11RenderSettings settings, DX11ObjectRenderSettings objectsettings)
        {
            for (int i = 0; i < this.variablesList.Count; i++)
            {
                this.variablesList[i].Apply(instance, settings, objectsettings);
            }
        }
    }
}
