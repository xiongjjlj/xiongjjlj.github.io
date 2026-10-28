using System;using System.IO;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Text;using System.Text.Json;
public static class OracleEntry {
 [DllImport("d3dcompiler_47.dll",CallingConvention=CallingConvention.StdCall)]static extern int D3DDisassemble(byte[] code,UIntPtr size,uint flags,IntPtr comments,out IntPtr blob);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate IntPtr GetBuffer(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate UIntPtr GetSize(IntPtr self);
 static string Disassemble(byte[] code,uint flags=0){IntPtr blob;int hr=D3DDisassemble(code,(UIntPtr)code.Length,flags,IntPtr.Zero,out blob);if(hr<0)Marshal.ThrowExceptionForHR(hr);try{IntPtr table=Marshal.ReadIntPtr(blob);var get=(GetBuffer)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table,3*IntPtr.Size),typeof(GetBuffer));var size=(GetSize)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table,4*IntPtr.Size),typeof(GetSize));int n=checked((int)size(blob).ToUInt64());var text=new byte[n];Marshal.Copy(get(blob),text,0,n);return Encoding.ASCII.GetString(text).TrimEnd('\0');}finally{Marshal.Release(blob);}}
 public static void Run(string[] args){
  var sources=new List<object>();var files=new List<string>(Directory.GetFiles(Path.GetDirectoryName(args[0]),"shader-sharedassets14-29-blob.bin"));if(files.Count>16)throw new Exception("Shader count exceeds budget");foreach(var file in files){
  byte[] raw=File.ReadAllBytes(file);if(raw.Length>1048576)throw new Exception("Shader input exceeds budget");var results=new List<object>();int count=BitConverter.ToInt32(raw,0);
  for(int i=0;i<count;i++){int offset=BitConverter.ToInt32(raw,4+12*i),length=BitConverter.ToInt32(raw,8+12*i);if(offset<0||length<0||offset+length>raw.Length)throw new Exception("Invalid program entry");for(int p=offset;p+32<=offset+length;p++){if(raw[p]!=68||raw[p+1]!=88||raw[p+2]!=66||raw[p+3]!=67)continue;int size=BitConverter.ToInt32(raw,p+24);if(size<32||p+size>offset+length)throw new Exception("Invalid DXBC length");var code=new byte[size];Array.Copy(raw,p,code,0,size);results.Add(new{blob_index=i,offset=p,bytes=size,assembly=Disassemble(code),assembly_hex=file.Contains("2-63-")?Disassemble(code,128):null});break;}}
  sources.Add(new{source=Path.GetFileName(file),entries=count,programs=results});}
  File.WriteAllText(args[0],JsonSerializer.Serialize(new{decoder="Windows system D3DDisassemble API",shaders=sources}));
 }
}
