In WinUI, a DataTemplateSelector is a class used to apply custom logic at runtime to choose which DataTemplate should be used for an item in an ItemsControl (like a ListView or ItemsRepeater). This allows items of the same type to be displayed differently based on their property values. [1, 2, 3]  

Implementation Steps 

To use a DataTemplateSelector in a WinUI application, you typically follow these steps: 

1. Define the Data Templates in XAML Resources

Define all the possible data templates as resources, usually in Page.Resources or Application.Resources. [1, 4, 5, 6]  

<Page.Resources>
    <DataTemplate x:DataType="local:MyDataItem" x:Key="TextTemplate">
        <TextBlock Text="{x:Bind Name}" Foreground="Black"/>
    </DataTemplate>
    <DataTemplate x:DataType="local:MyDataItem" x:Key="SeparatorTemplate">
        <Rectangle Height="2" Fill="Gray" Margin="0,5,0,5"/>
    </DataTemplate>
</Page.Resources>

2. Create a Custom Selector Class

Create a partial class in your code-behind that inherits from Microsoft.UI.Xaml.Controls.DataTemplateSelector and overrides the SelectTemplateCore methods. 

• Note: The SelectTemplateCore(object item) overload is generally preferred for most controls. Ensure your class is marked partial to work correctly with C#/WinRT in release builds. [2, 8, 9]  

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
// Add a using directive for your data model namespace

namespace YourAppName
{
    public partial class CustomTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate SeparatorTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is MyDataItem dataItem)
            {
                if (dataItem.IsSeparator)
                {
                    return SeparatorTemplate;
                }
                else
                {
                    return TextTemplate;
                }
            }
            return base.SelectTemplateCore(item);
        }
        
        // The other overload (with container) should typically just pass through 
        // to the base method as per Microsoft guidance.
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}

3. Instantiate the Selector in XAML and Assign Templates

Instantiate your custom selector class in your XAML resources and assign the previously defined DataTemplate resources to its properties. [7]  

<Page.Resources>
    <!-- ... (Data Templates from Step 1) ... -->

    <local:CustomTemplateSelector x:Key="MyCustomSelector"
                                  TextTemplate="{StaticResource TextTemplate}"
                                  SeparatorTemplate="{StaticResource SeparatorTemplate}" />
</Page.Resources>

4. Apply the Selector to an ItemsControl

Finally, set the ItemTemplateSelector property of your ItemsControl (e.g., ListView, GridView, ItemsRepeater) to your selector resource. [7, 10]  

For more detailed information and best practices, refer to the official Microsoft Learn documentation on Data template selection. 

<ItemsControl ItemsSource="{x:Bind MyCollection}"
              ItemTemplateSelector="{StaticResource MyCustomSelector}" />

AI responses may include mistakes.

[1] https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/data-template-selector
[2] https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.datatemplateselector?view=windows-app-sdk-1.8
[3] https://nicksnettravelswp.builttoroam.com/xaml-basics-datatemplateselector/
[4] https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/datatemplate?view=net-maui-10.0
[5] https://www.codeproject.com/articles/The-Guide-to-WinUI3-for-a-Cplusplus-Win32-Programm
[6] https://www.syncfusion.com/succinctly-free-ebooks/windowsphone8/the-user-interface-basic-xaml-concepts
[7] https://albertakhmetov.com/posts/2024/itemscontrol-and-datatemplateselector-in-winui/
[8] https://stackoverflow.com/questions/79767074/winui-3-datatemplateselector-always-crashed-when-using-aot
[9] https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.datatemplateselector.selecttemplatecore?view=winrt-26100
[10] https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.datatemplate?view=winrt-26100
