(function ($) {
    $.ajaxLoading = false;
    $.ajaxSubmit = function (url, data, callback, type, async, option) {
        if (async || !$.ajaxLoading) {
            var options = {
                url: url,
                data: data,
                type: type || 'POST',
                cache: false,
                timeout: 60000,
                beforeSend: function () { $.ajaxLoading = true; },
                complete: function () { $.ajaxLoading = false; },
                success: callback,
                error: callback
            };
            $.extend(options, option);
            $.ajax(options);
        }
    }
    $.getUrlParam = function (name) {
        var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
        var r = window.location.search.substr(1).match(reg);
        if (r != null) return unescape(r[2]); return null;
    }
    $.setUrlParam = function (url, name, value) {
        var r = url;
        if (r) {
            value = encodeURIComponent(value);
            var reg = new RegExp("(^|)" + name + "=([^&]*)(|$)");
            var tmp = name + "=" + value;
            if (url.match(reg) != null)
                r = url.replace(eval(reg), tmp);
            else if (url.match("[\?]"))
                r = url + "&" + tmp;
            else
                r = url + "?" + tmp;
        }
        return r;
    }
    $.compareMaxDate = function (maxDate, minDate, format) {
        if (typeof maxDate != "string" || typeof minDate != "string" || !format)
            return false;
        var min = minDate.toDate(format);
        var max = maxDate.toDate(format);
        return max > min;
    }
    $.compareMinDate = function (minDate, maxDate, format) {
        return $.compareMaxDate(maxDate, minDate, format);
    }
    $.getMaxDate = function (date1, date2, format) {
        if (typeof date1 != "string" || typeof date2 != "string" || !format)
            return "";
        var d1 = date1.toDate(format);
        var d2 = date2.toDate(format);
        return d1 > d2 ? date1 : date2;
    }
    $.getMinDate = function (date1, date2, format) {
        var max = $.getMaxDate(date1, date2, format);
        if (!max)
            return max;
        return max == date1 ? date2 : date1;
    }

    $.extend(String.prototype, {
        isNumber: function (value, element) {
            return (new RegExp(/^-?(?:\d+|\d{1,3}(?:,\d{3})+)(?:\.\d+)?$/).test(this));
        },
        startWith: function (pattern) {
            return this.indexOf(pattern) === 0;
        },
        endWith: function (pattern) {
            var d = this.length - pattern.length;
            return d >= 0 && this.lastIndexOf(pattern) === d;
        },
        replaceAll: function (os, ns) {
            return this.replace(new RegExp(os, "gm"), ns);
        },
        toDate: function (format) {
            var y = this.substring(format.indexOf('y'), format.lastIndexOf('y') + 1); //年
            var m = this.substring(format.indexOf('M'), format.lastIndexOf('M') + 1); //月
            var d = this.substring(format.indexOf('d'), format.lastIndexOf('d') + 1); //日
            if (isNaN(y)) y = new Date().getFullYear();
            if (isNaN(m)) m = new Date().getMonth();
            if (isNaN(d)) d = new Date().getDate();
            var h = this.substring(format.indexOf('h'), format.lastIndexOf('h') + 1);  //时
            var mi = this.substring(format.indexOf('m'), format.lastIndexOf('m') + 1); //分
            var s = this.substring(format.indexOf('s'), format.lastIndexOf('s') + 1);  //秒
            if (isNaN(h)) h = new Date().getHours();
            if (isNaN(mi)) mi = new Date().getMinutes();
            if (isNaN(s)) s = new Date().getSeconds();
            var dt;
            eval("dt = new Date('" + y + "', '" + (m - 1) + "','" + d + "','" + h + "','" + mi + "','" + s + "')");
            return dt;
        },
        trim: function (pattern) {
            var data = this;
            data = $.trim(data);
            if (pattern) {
                if (data.startWith(pattern))
                    data = data.substr(pattern.length, data.length - pattern.length);
                if (data.endWith(pattern))
                    data = data.substr(0, data.length - pattern.length);
            }
            return data;
        },
        isCellPhone: function () {
            return (new RegExp(/^1[0-9]{10}$/).test(this));
        },
        isTelephone: function () {
            return (new RegExp(/^((0\d{2,3})-)?(\d{7,8})(-(\d{3,}))?$/).test(this));
        },
        isZipCode: function () {
            return (new RegExp(/^[1-9][0-9]{5}$/).test(this));
        },
        isIdCardNo: function () {
            var area = { 11: "北京", 12: "天津", 13: "河北", 14: "山西", 15: "内蒙古", 21: "辽宁", 22: "吉林", 23: "黑龙江", 31: "上海", 32: "江苏", 33: "浙江", 34: "安徽", 35: "福建", 36: "江西", 37: "山东", 41: "河南", 42: "湖北", 43: "湖南", 44: "广东", 45: "广西", 46: "海南", 50: "重庆", 51: "四川", 52: "贵州", 53: "云南", 54: "西藏", 61: "陕西", 62: "甘肃", 63: "青海", 64: "宁夏", 65: "新疆", 71: "台湾", 81: "香港", 82: "澳门", 91: "国外" }
            if (area[parseInt(this.substr(0, 2))] == null)
                return false;//地区不合法

            switch (this.length) {
                case 15://15位身份证
                    var reg = /^[1-9][0-9]{5}[0-9]{2}((01|03|05|07|08|10|12)(0[1-9]|[1-2][0-9]|3[0-1])|(04|06|09|11)(0[1-9]|[1-2][0-9]|30)|02(0[1-9]|1[0-9]|2[0-8]))[0-9]{3}$/;
                    if ((parseInt(this.substr(6, 2)) + 1900) % 4 == 0 || ((parseInt(this.substr(6, 2)) + 1900) % 100 == 0 && (parseInt(this.substr(6, 2)) + 1900) % 4 == 0))
                        reg = /^[1-9][0-9]{5}[0-9]{2}((01|03|05|07|08|10|12)(0[1-9]|[1-2][0-9]|3[0-1])|(04|06|09|11)(0[1-9]|[1-2][0-9]|30)|02(0[1-9]|[1-2][0-9]))[0-9]{3}$/;

                    if (!reg.test(this))
                        return false;//出生日期不合法

                    break;
                case 18://18位身份证
                    //闰年月日:((01|03|05|07|08|10|12)(0[1-9]|[1-2][0-9]|3[0-1])|(04|06|09|11)(0[1-9]|[1-2][0-9]|30)|02(0[1-9]|[1-2][0-9]))
                    //平年月日:((01|03|05|07|08|10|12)(0[1-9]|[1-2][0-9]|3[0-1])|(04|06|09|11)(0[1-9]|[1-2][0-9]|30)|02(0[1-9]|1[0-9]|2[0-8]))
                    var reg = /^[1-9][0-9]{5}19[0-9]{2}((01|03|05|07|08|10|12)(0[1-9]|[1-2][0-9]|3[0-1])|(04|06|09|11)(0[1-9]|[1-2][0-9]|30)|02(0[1-9]|1[0-9]|2[0-8]))[0-9]{3}[0-9Xx]$/;//平年
                    if (parseInt(this.substr(6, 4)) % 4 == 0 || (parseInt(this.substr(6, 4)) % 100 == 0 && parseInt(this.substr(6, 4)) % 4 == 0))
                        reg = /^[1-9][0-9]{5}19[0-9]{2}((01|03|05|07|08|10|12)(0[1-9]|[1-2][0-9]|3[0-1])|(04|06|09|11)(0[1-9]|[1-2][0-9]|30)|02(0[1-9]|[1-2][0-9]))[0-9]{3}[0-9Xx]$/; //闰年

                    if (!reg.test(this))
                        return false;//出生日期不合法

                    var jym = "10X98765432";
                    var id_array = this.split("");
                    var s = (parseInt(id_array[0]) + parseInt(id_array[10])) * 7
                     + (parseInt(id_array[1]) + parseInt(id_array[11])) * 9
                     + (parseInt(id_array[2]) + parseInt(id_array[12])) * 10
                     + (parseInt(id_array[3]) + parseInt(id_array[13])) * 5
                     + (parseInt(id_array[4]) + parseInt(id_array[14])) * 8
                     + (parseInt(id_array[5]) + parseInt(id_array[15])) * 4
                     + (parseInt(id_array[6]) + parseInt(id_array[16])) * 2
                     + parseInt(id_array[7]) * 1
                     + parseInt(id_array[8]) * 6
                     + parseInt(id_array[9]) * 3;
                    var y = s % 11;
                    var m = jym.substr(y, 1);
                    if (m != id_array[17])
                        return false;//校验不合法

                    break;
                default: return false;//位数不合法
            }
            return true;
        }
    });

    $.extend(Date.prototype, {
        formatStr: function (format) {
            var o = {
                "M+": this.getMonth() + 1, //month
                "d+": this.getDate(),      //day
                "h+": this.getHours(),     //hour
                "m+": this.getMinutes(),   //minute
                "s+": this.getSeconds(),   //second
                "w+": "日一二三四五六".charAt(this.getDay()),   //week
                "q+": Math.floor((this.getMonth() + 3) / 3),  //quarter
                "S": this.getMilliseconds() //millisecond
            }
            if (/(y+)/.test(format)) {
                format = format.replace(RegExp.$1, (this.getFullYear() + "").substr(4 - RegExp.$1.length));
            }
            for (var k in o) {
                if (new RegExp("(" + k + ")").test(format)) {
                    format = format.replace(RegExp.$1, RegExp.$1.length == 1 ? o[k] : ("00" + o[k]).substr(("" + o[k]).length));
                }
            }
            return format;
        }
    });

    $.extend(Number.prototype, {
        formatMoney: function () {
            var value = Math.round(parseFloat(this) * 100) / 100;
            var array = value.toString().split(".");
            if (array.length == 1)
                value = value.toString() + ".00";
            else if (array[1].length < 2)
                value = value.toString() + "0";
            return "¥" + value;
        }
    });
})(jQuery);