using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GameLibrary.Logic;

namespace GameLibrary.Avalonia.Controls.SubPage.Indexer;

public partial class Indexer_File : UserControl
{
    private FileManager.ImportEntry_Binary? binary;

    public Indexer_File()
    {
        InitializeComponent();
    }

    public void Draw(FileManager.ImportEntry_Binary binary)
    {
        this.binary = binary;
        lbl_File.Content = binary.binaryLocation;
    }
}