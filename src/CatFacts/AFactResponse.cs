/*
 * MIT License
 *
 * Copyright (c) 2017 Katherine Marino
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;

namespace CatFacts {

    public abstract class AFactResponse { }

    public class FactResponseSay : AFactResponse {

        //--- Fields ---
        public string Text;

        //--- Constructors ---
        public FactResponseSay(string text) {
            Text = text;
        }
    }

    public class FactResponseDelay : AFactResponse {

        //--- Fields ---
        public readonly TimeSpan Delay;

        //--- Constructors ---
        public FactResponseDelay(TimeSpan delay) {
            Delay = delay;
        }
    }

    public class FactResponsePlay : AFactResponse {

        //--- Fields ---
        public readonly string Url;

        //--- Constructors ---
        public FactResponsePlay(string url) {
            Url = url;
        }
    }

    public class FactResponseNotUnderstood : AFactResponse { }

    public class FactResponseHelp : AFactResponse { }

    public class FactResponseBye : AFactResponse { }
}