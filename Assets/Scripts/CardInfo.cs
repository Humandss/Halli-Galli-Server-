using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//직렬화 및 역직렬화 과정에서 구조체를 사용하여 훨씬 용이하게 만들기 위해서 스크립트 분리
public struct CardInfo
{
  
    public Card.FruitType type;
    public int count;
    
   
}
