(function () {
    $(function () {
        $("#btn-save").click(save);
    })

    function save() {
        debugger;
        var files = $("#uploadFile")[0].files;
        if (!files || files.length == 0) {
            alert("You must upload a file.");
            return;
        }
        var formData = new FormData();
        formData.append("File", files[0]);
        var config = {
            headers: {
                'content-type': 'multipart/form-data'
            }
        };
        axios.post("/home/upload", formData, config )
            .then(function (response) {
                alert("Data imported successsfully.");
            })
            .catch(function (err) {
                console.log(err.response.data);
                alert(err.response.data);
            })
    }
})();
