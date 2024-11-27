
mergeInto(LibraryManager.library, {

	Inputing: function()
	{
		if(input != null)
		{
			SendMessage("IME_WEBGL_RECEIVER", "OnReceiveMessage",input.value);
		}
	},
	
	InputEnd: function()
	{
		if(input != null)
		{
			SendMessage("IME_WEBGL_RECEIVER","OnInputEnd");
		}
	},



	InputShow: function(v_, start, end)
	{
		var v = UTF8ToString(v_);
		if(typeof(input) === 'undefined' ||  input==null){
			input = document.createElement("input");
			input.type = "text";
			input.id = "IMEWebGlId";
			input.name = "IMEWebGl";
			input.style = "visibility:hidden;";
			input.oninput = _Inputing;
			input.onblur = _InputEnd;
			document.body.appendChild(input);
		}
		input.value = v;
		input.style.visibility = "visible";  
		input.style.opacity = 0;
   		input.focus();
		setTimeout(() => {
			input.setSelectionRange(start,end);
		}, 20);
	}
});