// SPDX-License-Identifier: Apache-2.0
package com.phonedesk.view;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import android.view.WindowManager;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

public class MainActivity extends Activity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        WebView web = new WebView(this);
        setContentView(web);
        WebSettings settings = web.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setUseWideViewPort(true);
        settings.setLoadWithOverviewMode(true);
        web.setWebViewClient(new WebViewClient() {
            @Override
            public void onReceivedError(WebView view, WebResourceRequest request, WebResourceError error) {
                Log.e("PhoneDeskView", "error code=" + error.getErrorCode()
                        + " desc=" + error.getDescription() + " url=" + request.getUrl());
            }
        });
        String token = getIntent().getStringExtra("token");
        if (token == null) token = "";
        String url = "http://localhost:8796/?t=" + token;
        Log.d("PhoneDeskView", "loading " + url);
        web.loadUrl(url);
    }
}
