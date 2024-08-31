
function getImageBase64DataURL(img) {
	var canvas = document.createElement('canvas');
    if (canvas && canvas.getContext) {
        canvas.width = img.width;
        canvas.height = img.height;
        var ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);        
        var ext = img.src.substring(img.src.lastIndexOf(".") + 1).toLowerCase();
        if (ext.indexOf('?') > -1) ext = 'png';
        var dataURL = canvas.toDataURL("image/" + ext);
        //var dataURL = canvas.toDataURL('image/png');
        return dataURL;
    }
}

function getBase64FromDataUrl(dataUrl) {
	if (!dataUrl) return null;
	return dataUrl.replace(/^data:image\/(\w+);base64,/g,'');
}
