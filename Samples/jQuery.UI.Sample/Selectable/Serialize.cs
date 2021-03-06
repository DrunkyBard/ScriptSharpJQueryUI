// Serialize.cs
// Script#/samples/jQuery.UI/jQuery.UI.Sample/Selectable
// Copyright (c) Ivaylo Gochkov, 2012
// Copyright (c) Microsoft Corporation.
// This source code is subject to terms and conditions of the Microsoft 
// Public License. A copy of the license can be found in License.txt.
//

using jQueryApi;
using jQueryApi.UI;
using System.Html;

namespace Sample.Selectable
{
    internal static class Serialize
    {
        static Serialize()
        {
            jQuery.OnDocumentReady(delegate()
            {
                jQuery.Select("#selectable2")
                      .Plugin<SelectableObject>()
                      .Selectable(new SelectableOptions(SelectableEvent.Stop,
                new SelectableStopEventHandler(delegate(jQueryEvent e, SelectableStopEvent uiEvent)
                {
                    jQueryObject result = jQuery.Select("#select-result").Empty();

                    jQuery.Select(".ui-selected", jQuery.Select("#selectable2")).Each(delegate(int idx, Element element)
                    {
                        int index = jQuery.Select("#selectable2 li").Index(element);
                        result.Append(" #" + (index + 1));
                    });
                })));
            });
        }
    }
}
