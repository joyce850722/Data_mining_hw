import java.io.BufferedReader;
import com.spreada.utils.chinese.ZHConverter;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.io.InputStream;

import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class hw2 {
	public String getHTML(String urlPath, String fmt){
        StringBuffer total = new StringBuffer();; 
        
        //獲取網頁HTML
        try{
            URL url = new URL(urlPath); 
            HttpURLConnection connection = (HttpURLConnection) url.openConnection();
            connection.connect(); 
            InputStream inStream = (InputStream) connection.getInputStream(); 
            BufferedReader reader = new BufferedReader(new InputStreamReader(inStream,fmt)); 
            String line=""; 
            
            while ((line = reader.readLine()) !=null ){ 
                total.append(line + "\n");
            } 
            //System.out.println("檔案："+total);
        }catch(IOException e){
            e.printStackTrace();
            System.out.println("取得網頁html時發生錯誤");
        }
        return total.toString();
    }

    /**
     * @param args
     * @throws IOException 
     */
    public static void main(String[] args) throws IOException {
    	String OR_URL_path = "http://www.rarbt.com/";
    	String res1 = "<div class=\"litpic\">+.*";//"subject\\/+[0-9]{5}.html";
    	
    	int count = 0;
    	String href[] = new String[30]; 
    	
        hw2 lf = new hw2();
        String html_data  = lf.getHTML(OR_URL_path,"utf8");
        //System.out.println(html_data);
        Pattern pattern = Pattern.compile(res1);
        Matcher matcher = pattern.matcher(html_data);
        
        boolean matchFound = matcher.find();
        int j=0;
        while(matchFound) {
          //System.out.println(matcher.start() + "-" + matcher.end());
          for(int i = 0; i <= matcher.groupCount(); i++) {
            String groupStr = matcher.group(i);
            //System.out.println(groupStr+"\n");
            
            String res2 = "subject\\/+[0-9]{5}.html";
            Pattern pattern2 = Pattern.compile(res2);
            Matcher matcher2 = pattern2.matcher(groupStr);
            boolean matchFound2 = matcher2.find();
            
            while(matchFound2) {
                //System.out.println(matcher.start() + "-" + matcher.end());
                for(int k = 0; k <= matcher2.groupCount(); k++) {
                	String groupStr2 = matcher2.group(k);
                	href[j] = OR_URL_path + groupStr2;
                	//System.out.println(j+":"+href[j]);
                }if(matcher2.end() + 1 <= groupStr.length()) {
	                matchFound2 = matcher2.find(matcher2.end());
	            }else{
	              break;
	            }
	          j++;
	          count++;
            }
            
          
          }
          if(matcher.end() + 1 <= html_data.length()) {
            matchFound = matcher.find(matcher.end());
              }else{
                break;
              }
        }
        
        //開始挖IMDB資料和影片名稱
        String IMDB[] = new String[count];
    	String name[] = new String[count];
        
        for(int i=0; i < href.length;i++){
        	html_data  = lf.getHTML(href[i],"utf8");
        	
        	res1 = "imdb=(.*)\">";
        	
        	pattern = Pattern.compile(res1);
            matcher = pattern.matcher(html_data);
            
            matchFound = matcher.find();
            
	        while(matchFound){
	          //System.out.println(matcher.start() + "-" + matcher.end());
	          for(j = 0; j <= matcher.groupCount(); j++) {
	        	  String groupStr = matcher.group(j);
	               
	              IMDB[i] = groupStr;
	              //System.out.println(i+" "+IMDB[i]);
	          }
	          if(matcher.end() + 1 <= html_data.length()) {
	        	  matchFound = matcher.find(matcher.end());
	              }else{
	            	  break;
	              }
	        }
	        
            res1 = "<h1>(.*)<\\/h1>";
            pattern = Pattern.compile(res1);
            matcher = pattern.matcher(html_data);
            
            matchFound = matcher.find();
            while(matchFound) {
              //System.out.println(matcher.start() + "-" + matcher.end());
              for(j = 0; j <= matcher.groupCount(); j++) {
                String groupStr = matcher.group(j);
                name[i] = ZHConverter.convert(groupStr, ZHConverter.TRADITIONAL);
                //System.out.println(i+" "+name[i]);
              }
              if(matcher.end() + 1 <= html_data.length()) {
                matchFound = matcher.find(matcher.end());
                  }else{
                    break;
                  }
            }
        }
        System.out.print("共有"+count+"筆影片資料!");
        FileWriter fw = new FileWriter("output.txt");
        fw.write("共有"+count+"筆影片資料!");
        for(int i=0;i<count;i++){
        	System.out.print("\r\n\r\n"+(i+1)+"\r\n片名："+name[i]+"\r\n影片下載連結："+href[i]+"\r\nIMDB："+IMDB[i]);
        	fw.write("\r\n\r\n"+(i+1)+"\r\n片名："+name[i]+"\r\n影片下載連結："+href[i]+"\r\nIMDB："+IMDB[i]);
        }
        
        fw.flush();
        fw.close();
    }
}
