# PrivateVisualStudioGalleryGenerator
Generator to create a Private Visual Studio Extension Gallery

Original Code from Mark Miller at DevExpress [Link](https://community.devexpress.com/blogs/markmiller/archive/2017/08/14/how-visual-studio-s-private-gallery-helps-us-create-a-better-product.aspx)

[Changelog](CHANGELOG.md)

# Todo
* Move to dotnetCore

# Documentation
Use the built executable with the following parameters:

#### Local:  
`UpdateAtomFeed.exe %pathToVsixFile% %pathOfPrivateGallery%`

#### Network:  
`UpdateAtomFeed.exe %pathToVsixFile% %networkPath% %userName% %password%`  
The credetials will be used if the `%networkPath%` starts with `\`
