/*
 * Copyright 2014, Gregg Tavares.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 *     * Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above
 * copyright notice, this list of conditions and the following disclaimer
 * in the documentation and/or other materials provided with the
 * distribution.
 *     * Neither the name of Gregg Tavares. nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace DeJson {

public class Deserializer {

    /// <summary>
    /// A Class used to direct which class to make when it's not obvious, like if you have a class
    /// with a member that's a base class but the actual class could be one of many derived classes.
    /// </summary>
    public abstract class CustomCreator {

        /// <summary>
        /// Creates an new derived class when a base is expected
        /// </summary>
        /// <param name="src">A dictionary of the json fields that belong to the object to be created.</param>
        /// <param name="parentSrc">A dictionary of the json fields that belong to the object that is the parent of the object to be created.</param>
        /// <example>
        /// Example: Assume you have the following classes
        /// <code>
        ///     class Fruit { public int type; }
        ///     class Apple : Fruit { public float height; public float radius; };
        ///     class Raspberry : Fruit { public int numBulbs; }
        /// </code>
        /// You'd register a dervied CustomCreator for type `Fruit`. When the Deserialize needs to create
        /// a `Fruit` it will call your Create function. Using `src` you could look at `type` and
        /// decide whether to make an Apple or a Raspberry.
        /// <code>
        ///     int type = src["type"];
        ///     if (type == 0) { return new Apple; }
        ///     if (type == 1) { return new Raspberry; }
        ///     ..
        /// </code>
        /// If the parent has info on the type you can do this
        /// <code>
        ///     class Fruit { }
        ///     class Apple : Fruit { public float height; public float radius; };
        ///     class Raspberry : Fruit { public int numBulbs; }
        ///     class Basket { public int typeInBasket; Fruit fruit; }
        /// </code>
        /// In this case again, when trying to create a `Fruit` your CustomCreator.Create function
        /// will be called. You can use `'parentSrc`' to look at the fields from 'Basket' as in
        /// <code>
        ///     int typeInBasket = parentSrc['typeInBasket'];
        ///     if (type == 0) { return new Apple; }
        ///     if (type == 1) { return new Raspberry; }
        ///     ..
        /// </code>
        /// </example>
        /// <returns>The created object</returns>
        public abstract object Create(Dictionary<string, object> src, Dictionary<string, object> parentSrc);

        /// <summary>
        /// The base type this CustomCreator makes.
        /// </summary>
        /// <returns>The type this CustomCreator makes.</returns>
        public abstract System.Type TypeToCreate();
    }

    /// <summary>
    /// Deserializer for Json to your classes.
    /// </summary>
    public Deserializer() {
        m_creators = new Dictionary<System.Type, CustomCreator>();
    }

    /// <summary>
    /// Deserializes a json string into classes.
    /// </summary>
    /// <param name="json">String containing JSON</param>
    /// <returns>An instance of class T.</returns>
    /// <example>
    /// <code>
    ///     public class Foo {
    ///         public int num;
    ///         public string name;
    ///         public float weight;
    ///     };
    ///
    ///     public class Bar {
    ///         public int hp;
    ///         public Foo someFoo;
    ///     };
    /// ...
    ///     Deserializer deserializer = new Deserializer();
    ///
    ///     string json = "{\"hp\":123,\"someFoo\":{\"num\":456,\"name\":\"gman\",\"weight\":156.4}}";
    ///
    ///     Bar bar = deserializer.Deserialize<Bar>(json);
    ///
    ///     print("bar.hp: " + bar.hp);
    ///     print("bar.someFoo.num: " + bar.someFoo.num);
    ///     print("bar.someFoo.name: " + bar.someFoo.name);
    ///     print("bar.someFoo.weight: " + bar.someFoo.weight);
    ///
    /// </code>
    /// </example>
    public T Deserialize<T> (string json) where T : new() {
        Dictionary<string, object> src = (Dictionary<string, object>)Json.Deserialize(json);
        return DeserializeT<T>(src);
    }

    /// <summary>
    /// Registers a CustomCreator.
    /// </summary>
    /// <param name="creator">The creator to register</param>
    public void RegisterCreator(CustomCreator creator) {
        System.Type t = creator.TypeToCreate();
        m_creators[t] = creator;
    }

    private T DeserializeT<T>(Dictionary<string, object> src) where T : new() {
        object o = DeserializeO(typeof(T), src, null);
        return (T)o;
    }

    private object DeserializeO(Type destType, Dictionary<string, object> src, Dictionary<string, object> parentSrc) {
        object dest = null;

        CustomCreator creator;
        if (m_creators.TryGetValue(destType, out creator)) {
            dest = creator.Create(src, parentSrc);
        }

        if (dest == null) {
            dest = Activator.CreateInstance(destType);
        }

        DeserializeIt(dest, src);
        return dest;
    }

    private void DeserializeIt(object dest, Dictionary<string, object> src) {
        System.Type type = dest.GetType();
        System.Reflection.FieldInfo[] fields = type.GetFields();

        DeserializeClassFields(dest, fields, src);
    }

    private void DeserializeClassFields(object dest, System.Reflection.FieldInfo[] fields, Dictionary<string, object> src) {
        foreach (System.Reflection.FieldInfo info in fields) {

            object value = src[info.Name];
            DeserializeField(dest, info, value, src);

        }
    }

    private void DeserializeField(object dest, System.Reflection.FieldInfo info, object value, Dictionary<string, object> src) {
        Type fieldType = info.FieldType;
        object o = ConvertToType(value, fieldType, src);
        info.SetValue(dest, o);
    }

    private object ConvertToType(object value, System.Type type, Dictionary<string, object> src) {
        if (type.IsArray) {
            List<object> elements = (List<object>)value;
            int numElements = elements.Count;
            Type elementType = type.GetElementType();
            Array array = Array.CreateInstance(elementType, numElements);
            int index = 0;
            foreach (object elementValue in elements) {
                object o = ConvertToType(elementValue, elementType, src);
                array.SetValue(o, index);
                ++index;
            }
            return array;
        } else if (type == typeof(string)) {
            return Convert.ToString(value);
        } else if (type == typeof(int)) {
            return Convert.ToInt32(value);
        } else if (type == typeof(float)) {
            return Convert.ToSingle(value);
        } else if (type == typeof(double)) {
            return Convert.ToDouble(value);
        } else if (type == typeof(bool)) {
            return Convert.ToBoolean(value);
        } else if (type.IsClass) {
            return DeserializeO(type, (Dictionary<string, object>)value, src);
        } else {
            // Should we throw here?
        }
        return value;
    }

    private Dictionary<System.Type, CustomCreator> m_creators;
};

}  // namespace DeJson


